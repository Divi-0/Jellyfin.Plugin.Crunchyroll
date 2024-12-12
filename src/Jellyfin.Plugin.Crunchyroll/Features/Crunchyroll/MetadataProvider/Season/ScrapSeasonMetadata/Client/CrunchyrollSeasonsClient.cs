using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client;

public sealed class CrunchyrollSeasonsClient : ICrunchyrollSeasonsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    private readonly ILogger<CrunchyrollSeasonsClient> _logger;

    public CrunchyrollSeasonsClient(HttpClient httpClient, PluginConfiguration config, 
        ICrunchyrollSessionRepository crunchyrollSessionRepository, ILogger<CrunchyrollSeasonsClient> logger)
    {
        _httpClient = httpClient;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(config.CrunchyrollUrl);
    }
    
    public async Task<Result<CrunchyrollSeasonsResponse>> GetSeasonsAsync(string titleId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        var locacle = language.Name;
        var path =
            $"content/v2/cms/series/{titleId}/seasons?force_locale=&locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(SeasonsErrorCodes.NoSession);
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
            _logger.LogError(e, "request for titleId {TitleId} was not successful", 
                titleId);
            return Result.Fail(SeasonsErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for titleId {TitleId} was not successful. StatusCode: {StatusCode}", 
                titleId, response.StatusCode);
            return Result.Fail(SeasonsErrorCodes.RequestFailed);
        }
        
        CrunchyrollSeasonsResponse? seasonsResponse;
        try
        {
            seasonsResponse = await response.Content.ReadFromJsonAsync<CrunchyrollSeasonsResponse>(cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "invalid json format");
            return Result.Fail(SeasonsErrorCodes.InvalidResponse);
        }

        if (seasonsResponse is null)
        {
            _logger.LogError("invalid json format");
            return Result.Fail(SeasonsErrorCodes.InvalidResponse);
        }
        
        return seasonsResponse;
    }
}