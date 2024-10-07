using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Mappings;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

public sealed class CrunchyrollGetReviewsClient : ICrunchyrollGetReviewsClient
{
    private readonly HttpClient _httpClient;
    private readonly PluginConfiguration _pluginConfiguration;
    private readonly ILogger<CrunchyrollGetReviewsClient> _logger;
    private readonly ICrunchyrollSessionRepository _sessionRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CrunchyrollGetReviewsClient(HttpClient httpClient, PluginConfiguration pluginConfiguration, ILogger<CrunchyrollGetReviewsClient> logger,
        ICrunchyrollSessionRepository sessionRepository)
    {
        _httpClient = httpClient;
        _pluginConfiguration = pluginConfiguration;
        _logger = logger;
        _sessionRepository = sessionRepository;

        _httpClient.BaseAddress =
            new Uri(pluginConfiguration.CrunchyrollUrl);

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    public async Task<Result<ReviewsResponse>> GetReviewsAsync(string titleId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var locacle = new CultureInfo(_pluginConfiguration.CrunchyrollLanguage).Name;
        var path =
            $"content-reviews/v2/{locacle}/review/series/{titleId}/list?page={pageNumber}&page_size={pageSize}&sort=helpful";

        var bearerToken = await _sessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return Result.Fail(GetReviewsErrorCodes.NoSession);
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
            _logger.LogError("request for titleId {TitleId} with pageNumber/Size {PageNumber}/{PageSize} was not successful. StatusCode: {StatusCode}", 
                titleId, pageNumber, pageSize, response.StatusCode);
            return Result.Fail(GetReviewsErrorCodes.RequestFailed);
        }

        CrunchyrollReviewsResponse? crunchyrollReviews;
        try
        {
            crunchyrollReviews = await response.Content.ReadFromJsonAsync<CrunchyrollReviewsResponse>(cancellationToken);
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "invalid json format");
            return Result.Fail(GetReviewsErrorCodes.InvalidResponse);
        }

        if (crunchyrollReviews is null)
        {
            return Result.Fail(GetReviewsErrorCodes.InvalidResponse);
        }

        try
        {
            var reviewsResponse = crunchyrollReviews.ToReviewsResponse("https://static.crunchyroll.com/assets/avatar/170x170");
            return reviewsResponse;
        }
        catch (ArgumentOutOfRangeException e)
        {
            _logger.LogError(e, "reviews response mapping failed");
            return Result.Fail(GetReviewsErrorCodes.MappingFailed);
        }
    }
}