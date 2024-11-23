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
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId.Client;

public class CrunchyrollMovieEpisodeIdClient : ICrunchyrollMovieEpisodeIdClient
{
    private readonly HttpClient _httpClient;
    private readonly PluginConfiguration _pluginConfiguration;
    private readonly ILogger<CrunchyrollMovieEpisodeIdClient> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CrunchyrollMovieEpisodeIdClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollMovieEpisodeIdClient> logger,
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
    
    public async Task<Result<SearchResponse?>> SearchTitleIdAsync(string name, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching episode id for {Name}", name);
        
        var urlEncodedName = UrlEncoder.Default.Encode(name);

        var locacle = new CultureInfo(_pluginConfiguration.CrunchyrollLanguage).Name;
        var path =
            $"content/v2/discover/search?q={urlEncodedName}&n=6&type=episode&ratings=true&locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(Domain.Constants.ErrorCodes.CrunchyrollSessionMissing);
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
            return Result.Fail(Domain.Constants.ErrorCodes.CrunchyrollSearchFailed);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var crunchyrollSearchResponse = JsonSerializer.Deserialize<CrunchyrollSearchResponse>(content, _jsonSerializerOptions);

        if (crunchyrollSearchResponse is null)
        {
            return Result.Fail(Domain.Constants.ErrorCodes.CrunchyrollSearchContentIncompatible);
        }
        
        foreach (var searchData in crunchyrollSearchResponse.Data)
        {
            foreach (var item in searchData.Items)
            {
                var regex = new Regex(name.Replace(" ", ".*"));
                if (regex.IsMatch(item.Title) && item.EpisodeMetadata is not null)
                {
                    return new SearchResponse
                    {
                        SeriesId = item.EpisodeMetadata.SeriesId,
                        SeriesSlugTitle = item.EpisodeMetadata.SeriesSlugTitle,
                        SeasonId = item.EpisodeMetadata.SeasonId,
                        EpisodeId = item.Id,
                        EpisodeSlugTitle = item.SlugTitle,
                    };
                }
            }
        }
        
        _logger.LogDebug("No episode id movie with name {Name} was found. response {@Response}", name, crunchyrollSearchResponse);
        
        return Result.Ok<SearchResponse?>(null);
    }
}