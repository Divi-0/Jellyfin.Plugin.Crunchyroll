using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Domain.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId.Client;

public class CrunchyrollTitleIdClient : ICrunchyrollTitleIdClient
{
    private readonly HttpClient _httpClient;
    private readonly PluginConfiguration _pluginConfiguration;
    private readonly ILogger<CrunchyrollTitleIdClient> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CrunchyrollTitleIdClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollTitleIdClient> logger,
        ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _httpClient = httpClient;
        _pluginConfiguration = pluginConfiguration;
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
    public async Task<Result<SearchResponse?>> GetTitleIdAsync(string title, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching titleId for {Title}", title);
        
        var urlEncodedTitle = UrlEncoder.Default.Encode(title);

        var locacle = new CultureInfo(_pluginConfiguration.CrunchyrollLanguage).Name;
        var path =
            $"content/v2/discover/search?q={urlEncodedTitle}&n=6&type=series,movie_listing&ratings=true&locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
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
            return Result.Fail(ErrorCodes.CrunchyrollSearchContentIncompatible);
        }
        
        foreach (var searchData in crunchyrollSearchResponse.Data)
        {
            foreach (var item in searchData.Items)
            {
                if (item.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    return new SearchResponse()
                    {
                        Id = item.Id,
                        SlugTitle = item.SlugTitle
                    };
                }
            }
        }
        
        _logger.LogDebug("No title id for {Title} was found", title);
        
        return Result.Ok<SearchResponse?>(null);
    }
}