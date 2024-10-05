using System.Globalization;
using System.Net;
using System.Text.Json;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Microsoft.Net.Http.Headers;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ScrapTitleMetadata.MockHelper;

public static class MockHttpResponse
{
    public static MockedRequest MockCrunchyrollSeasonsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, CrunchyrollSeasonsResponse response)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}/seasons?force_locale=&locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(response));

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeasonsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, HttpStatusCode statusCode)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}/seasons?force_locale=&locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(statusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeasonsResponseThrows(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, Exception exception)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}/seasons?force_locale=&locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Throw(exception);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeasonsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, string json)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}/seasons?force_locale=&locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", json);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollEpisodesResponse(this MockHttpMessageHandler mockHttpMessageHandler, string seasonId,
        CultureInfo language, string bearerToken, CrunchyrollEpisodesResponse response)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/seasons/{seasonId}/episodes?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(response));

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollEpisodesResponse(this MockHttpMessageHandler mockHttpMessageHandler, string seasonId,
        CultureInfo language, string bearerToken, HttpStatusCode statusCode)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/seasons/{seasonId}/episodes?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(statusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollEpisodesResponseThrows(this MockHttpMessageHandler mockHttpMessageHandler, string seasonId,
        CultureInfo language, string bearerToken, Exception exception)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/seasons/{seasonId}/episodes?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Throw(exception);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollEpisodesResponse(this MockHttpMessageHandler mockHttpMessageHandler, string seasonId,
        CultureInfo language, string bearerToken, string json)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/seasons/{seasonId}/episodes?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", json);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeriesResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, CrunchyrollSeriesContentResponse response)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", JsonSerializer.Serialize(response));

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeriesResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, HttpStatusCode statusCode)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond(statusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeriesResponseThrows(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, Exception exception)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Throw(exception);

        return mockedRequest;
    }
    
    public static MockedRequest MockCrunchyrollSeriesResponse(this MockHttpMessageHandler mockHttpMessageHandler, string titleId,
        CultureInfo language, string bearerToken, string json)
    {
        var url = $"https://www.crunchyroll.com/content/v2/cms/series/{titleId}?locale={language.Name}";
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .WithHeaders(HeaderNames.Authorization, $"Bearer {bearerToken}")
            .Respond("application/json", json);

        return mockedRequest;
    }
}