using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;

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
    }
    
    public async Task<Result<CrunchyrollSeriesContentItem>> GetSeriesMetadataAsync(string titleId, CultureInfo language, CancellationToken cancellationToken)
    {
        var locacle = language.Name;
        var path =
            $"content/v2/cms/series/{titleId}?locale={locacle}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(SeriesErrorCodes.NoSession);
        }
        
        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri($"{_config.CrunchyrollUrl}{path}", UriKind.Absolute),
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
        
        return seriesMetadataResponse.Data[0];
    }

    public async Task<Result<Stream>> GetPosterImagesAsync(string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(new Uri(url, UriKind.Absolute), cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "request for image {url} was not successful", 
                url);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for image {url} was not successful. StatusCode: {StatusCode}", 
                url, response.StatusCode);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }
        
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<Result<float>> GetRatingAsync(string titleId, CancellationToken cancellationToken)
    {
        var path =
            $"content-reviews/v2/rating/series/{titleId}";

        var bearerToken = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(SeriesErrorCodes.NoSession);
        }
        
        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri($"{_config.CrunchyrollUrl}{path}", UriKind.Absolute),
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
            _logger.LogError(e, "request for title rating with titleId {TitleId} was not successful", 
                titleId);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("request for title rating with titleId {TitleId} was not successful. StatusCode: {StatusCode}", 
                titleId, response.StatusCode);
            return Result.Fail(SeriesErrorCodes.RequestFailed);
        }
        
        CrunchyrollSeriesRatingResponse? seriesRatingResponse;
        try
        {
            seriesRatingResponse = await response.Content.ReadFromJsonAsync<CrunchyrollSeriesRatingResponse>(cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "invalid json format");
            return Result.Fail(SeriesErrorCodes.InvalidResponse);
        }

        if (seriesRatingResponse is null)
        {
            _logger.LogError("invalid json format");
            return Result.Fail(SeriesErrorCodes.InvalidResponse);
        }

        var isParsed = float.TryParse(seriesRatingResponse.Average, CultureInfo.InvariantCulture, out var rating);

        if (!isParsed)
        {
            _logger.LogError("value {Value} could not be parsed to float", seriesRatingResponse.Average);
        }
        
        return float.Round(rating, 1); //return 0.0f if was not parsable
    }
}