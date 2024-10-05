using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;

public class CrunchyrollSeriesClient : ICrunchyrollSeriesClient
{
    private readonly HttpClient _httpClient;
    private readonly PluginConfiguration _config;
    private readonly ILogger<CrunchyrollSeriesClient> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public CrunchyrollSeriesClient(HttpClient httpClient, PluginConfiguration config,
        ILogger<CrunchyrollSeriesClient> logger, ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
        
        _httpClient.BaseAddress = new Uri(_config.CrunchyrollUrl);
    }
    
    public async Task<Result<CrunchyrollSeriesContentResponse>> GetSeriesMetadataAsync(string titleId, CancellationToken cancellationToken)
    {
        var locacle = new CultureInfo(_config.CrunchyrollLanguage).Name;
        var path =
            $"content/v2/cms/series/{titleId}?locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(SeriesErrorCodes.NoSession);
        }
        
        var requestMessage = new HttpRequestMessage
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
            _logger.LogError(e, "request for title metadata with titleId {TitleId} was not successful", 
                titleId);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for title metadata with titleId {TitleId} was not successful. StatusCode: {StatusCode}", 
                titleId, response.StatusCode);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }
        
        CrunchyrollSeriesContentResponse? seriesMetadataResponse;
        try
        {
            seriesMetadataResponse = await response.Content.ReadFromJsonAsync<CrunchyrollSeriesContentResponse>(cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "invalid json format");
            return Result.Fail(SeriesErrorCodes.InvalidResponse);
        }

        if (seriesMetadataResponse is null)
        {
            _logger.LogError("invalid json format");
            return Result.Fail(SeriesErrorCodes.InvalidResponse);
        }
        
        return seriesMetadataResponse;
    }

    public async Task<Result<Stream>> GetPosterImagesAsync(CrunchyrollSeriesImage image, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(image.Source, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "request for image {url} was not successful", 
                image.Source);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for image {url} was not successful. StatusCode: {StatusCode}", 
                image.Source, response.StatusCode);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }
        
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}