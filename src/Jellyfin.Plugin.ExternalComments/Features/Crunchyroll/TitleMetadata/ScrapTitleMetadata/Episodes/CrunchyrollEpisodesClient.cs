using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;

public sealed class CrunchyrollEpisodesClient : ICrunchyrollEpisodesClient
{
    private readonly HttpClient _httpClient;
    private readonly PluginConfiguration _config;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    private readonly ILogger<CrunchyrollEpisodesClient> _logger;

    public CrunchyrollEpisodesClient(HttpClient httpClient, PluginConfiguration config, 
        ICrunchyrollSessionRepository crunchyrollSessionRepository, ILogger<CrunchyrollEpisodesClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_config.CrunchyrollUrl);
    }

    public async Task<Result<CrunchyrollEpisodesResponse>> GetEpisodesAsync(string seasonId, CancellationToken cancellationToken)
    {
        var locacle = new CultureInfo(_config.CrunchyrollLanguage).Name;
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
}