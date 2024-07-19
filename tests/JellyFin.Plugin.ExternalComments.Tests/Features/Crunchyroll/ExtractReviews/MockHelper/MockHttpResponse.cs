using System.Net;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews.MockHelper;

public static class MockHttpResponse
{
    public static MockedRequest MockWaybackMachineUrlHtmlReviewsResponse(this MockHttpMessageHandler mockHttpMessageHandler, string url)
    {
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .Respond("text/html", Properties.Resources.WaybackHtmlCrunchyrollReviews);

        return mockedRequest;
    }
    
    public static MockedRequest MockWaybackMachineUrlHtmlReviewsResponseFails(this MockHttpMessageHandler mockHttpMessageHandler, string url,
        HttpStatusCode statusCode)
    {
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .Respond(statusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockWaybackMachineUrlHtmlReviewsResponseThrows(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        Exception exception)
    {
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .Throw(exception);

        return mockedRequest;
    }
    
    public static MockedRequest MockWaybackMachineUrlHtmlReviewsResponseInvalidHtml(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        Exception exception)
    {
        var mockedRequest = mockHttpMessageHandler
            .When(url)
            .Respond("text/html", Properties.Resources.WaybackHtmlCrunchyrollReviewsWithoutReviewsClass);

        return mockedRequest;
    }
}