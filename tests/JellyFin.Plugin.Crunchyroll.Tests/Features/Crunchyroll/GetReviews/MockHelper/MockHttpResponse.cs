using System.Globalization;
using System.Net;
using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;
using Microsoft.Net.Http.Headers;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.GetReviews.MockHelper;

public static class MockHttpResponse
{
    public static MockHttpMessageHandler MockCrunchyrollReviewsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, int pageNumber, int pageSize, string bearerToken, CrunchyrollReviewsResponse response)
    {
        var url = $"https://www.crunchyroll.com/content-reviews/v2/{language.Name}/review/series/{titleId}/list?page={pageNumber}&page_size={pageSize}&sort=helpful";
        mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(response));

        return mockHttpMessageHandler;
    }
    
    public static MockHttpMessageHandler MockCrunchyrollReviewsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, int pageNumber, int pageSize, string bearerToken, string json)
    {
        var url = $"https://www.crunchyroll.com/content-reviews/v2/{language.Name}/review/series/{titleId}/list?page={pageNumber}&page_size={pageSize}&sort=helpful";
        mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", json);

        return mockHttpMessageHandler;
    }
    
    public static MockHttpMessageHandler MockCrunchyrollReviewsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, int pageNumber, int pageSize, string bearerToken, HttpStatusCode statusCode)
    {
        var url = $"https://www.crunchyroll.com/content-reviews/v2/{language.Name}/review/series/{titleId}/list?page={pageNumber}&page_size={pageSize}&sort=helpful";
        mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(statusCode);

        return mockHttpMessageHandler;
    }
}