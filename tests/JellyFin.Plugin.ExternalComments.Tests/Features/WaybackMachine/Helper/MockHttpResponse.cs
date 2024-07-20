using System.Net;
using System.Text.Json;
using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.WaybackMachine.Helper;

public static class MockHttpResponse
{
    public static (MockedRequest mockedRequest, AvailabilityResponse response) MockGetAvailableRequest(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, string response = "")
    {
        var fixture = new Fixture();
        
        var availabilityResponse = fixture.Create<AvailabilityResponse>();

        if (string.IsNullOrWhiteSpace(response))
        {
            response = JsonSerializer.Serialize(availabilityResponse);
        }
        
        
        var fullUrl = $"https://archive.org/wayback/available?url={url}&timestamp={timeStamp.ToString("yyyyMMddhhmmss")}&timeout=180&closest=either&status_code=200";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond("application/json", response);

        return (mockedRequest, availabilityResponse);
    }
    
    public static MockedRequest MockGetAvailableRequestFails(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, HttpStatusCode httpStatusCode)
    {
        var fullUrl = $"https://archive.org/wayback/available?url={url}&timestamp={timeStamp.ToString("yyyyMMddhhmmss")}&timeout=180&closest=either&status_code=200";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond(httpStatusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockGetAvailableRequestNullResponse(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp)
    {
        var fullUrl = $"https://archive.org/wayback/available?url={url}&timestamp={timeStamp.ToString("yyyyMMddhhmmss")}&timeout=180&closest=either&status_code=200";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond("application/json", JsonSerializer.Serialize<AvailabilityResponse>(null!));

        return mockedRequest;
    }
}