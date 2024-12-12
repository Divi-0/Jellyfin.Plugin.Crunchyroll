using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId.Client;

public class CrunchyrollSeriesIdClient : ICrunchyrollSeriesIdClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrunchyrollSeriesIdClient> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CrunchyrollSeriesIdClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollSeriesIdClient> logger,
        ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;

        _httpClient.BaseAddress =
            new Uri(pluginConfiguration.CrunchyrollUrl);

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    /// <inheritdoc />
    public async Task<Result<CrunchyrollId?>> GetSeriesIdAsync(string title, CultureInfo language, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching titleId for {Title}", title);
        
        var urlEncodedTitle = UrlEncoder.Default.Encode(title);

        var locacle = language.Name;
        var path =
            $"content/v2/discover/search?q={urlEncodedTitle}&n=6&type=series,movie_listing&ratings=true&locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            _logger.LogError("no session found");
            return Result.Fail(ErrorCodes.CrunchyrollSessionMissing);
        }

        var requestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri(path, UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = { { HeaderNames.Authorization, $"Bearer {bearerToken}" } }
        };
        
        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Crunchyroll search failed");
            return Result.Fail(ErrorCodes.CrunchyrollSearchFailed);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var crunchyrollSearchResponse = JsonSerializer.Deserialize<CrunchyrollSearchResponse>(content, _jsonSerializerOptions);

        if (crunchyrollSearchResponse is null)
        {
            _logger.LogError("crunchyroll search response for title {Title} was empty", title);
            return Result.Fail(ErrorCodes.CrunchyrollSearchContentIncompatible);
        }
        
        var regex = new Regex($"^{title.Replace(" ", ".*")}.?$", RegexOptions.IgnoreCase);
        foreach (var searchData in crunchyrollSearchResponse.Data)
        {
            foreach (var item in searchData.Items)
            {
                if (regex.IsMatch(item.Title))
                {
                    return new CrunchyrollId(item.Id);
                }
            }
        }
        
        _logger.LogDebug("No title id for {Title} was found. response {@Response}", title, crunchyrollSearchResponse);
        
        return Result.Ok<CrunchyrollId?>(null);
    }
}