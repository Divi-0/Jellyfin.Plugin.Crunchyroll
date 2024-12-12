using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;

public sealed class ScrapEpisodeCrunchyrollClient : IScrapEpisodeCrunchyrollClient
{
    private readonly HttpClient _httpClient;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    private readonly ILogger<ScrapEpisodeCrunchyrollClient> _logger;

    public ScrapEpisodeCrunchyrollClient(HttpClient httpClient, PluginConfiguration config, 
        ICrunchyrollSessionRepository crunchyrollSessionRepository, ILogger<ScrapEpisodeCrunchyrollClient> logger)
    {
        _httpClient = httpClient;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(config.CrunchyrollUrl);
    }

    public async Task<Result<CrunchyrollEpisodesResponse>> GetEpisodesAsync(string seasonId, CultureInfo language, CancellationToken cancellationToken)
    {
        var locacle = language.Name;
        var path = $"content/v2/cms/seasons/{seasonId}/episodes?locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(EpisodesErrorCodes.NoSession);
        }
        
        var requestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri(path, UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = { { HeaderNames.Authorization, $"Bearer {bearerToken}" } }
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "request for titleId {SeasonId} was not successful", 
                seasonId);
            return Result.Fail(EpisodesErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for titleId {SeasonId} was not successful. StatusCode: {StatusCode}", 
                seasonId, response.StatusCode);
            return Result.Fail(EpisodesErrorCodes.RequestFailed);
        }
        
        CrunchyrollEpisodesResponse? seasonsResponse;
        try
        {
            seasonsResponse = await response.Content.ReadFromJsonAsync<CrunchyrollEpisodesResponse>(cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "invalid json format");
            return Result.Fail(EpisodesErrorCodes.InvalidResponse);
        }

        if (seasonsResponse is null)
        {
            _logger.LogError("invalid json format");
            return Result.Fail(EpisodesErrorCodes.InvalidResponse);
        }
        
        return seasonsResponse;
    }

    public async Task<Result<CrunchyrollEpisodeDataItem>> GetEpisodeAsync(string episodeId, CultureInfo language, CancellationToken cancellationToken)
    {
        var locacle = language.Name;
        var path = $"content/v2/cms/objects/{episodeId}?ratings=true&locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(EpisodesErrorCodes.NoSession);
        }
        
        var requestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri(path, UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = { { HeaderNames.Authorization, $"Bearer {bearerToken}" } }
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "request for episode with id {EpisodeId} was not successful", 
                episodeId);
            return Result.Fail(EpisodesErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for episode with id {EpisodeOd} was not successful. StatusCode: {StatusCode}", 
                episodeId, response.StatusCode);
            return Result.Fail(EpisodesErrorCodes.RequestFailed);
        }
        
        CrunchyrollEpisodeResponse? episodeResponse;
        try
        {
            episodeResponse = await response.Content.ReadFromJsonAsync<CrunchyrollEpisodeResponse>(cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "invalid json format");
            return Result.Fail(EpisodesErrorCodes.InvalidResponse);
        }

        if (episodeResponse is null)
        {
            _logger.LogError("invalid json format");
            return Result.Fail(EpisodesErrorCodes.InvalidResponse);
        }
        
        return episodeResponse.Data[0];
    }
}