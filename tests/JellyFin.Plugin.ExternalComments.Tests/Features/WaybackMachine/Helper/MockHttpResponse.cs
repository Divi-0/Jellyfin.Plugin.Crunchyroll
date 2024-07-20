using System.Net;
using System.Text.Json;
using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.WaybackMachine.Helper;

public static class MockHttpResponse
{
    public static (MockedRequest mockedRequest, SearchResponse response) MockSearchRequest(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, string response = "")
    {
        var fixture = new Fixture();
        
        var searchResponse = fixture.Create<SearchResponse>();

        if (string.IsNullOrWhiteSpace(response))
        {
            response = JsonSerializer.Serialize<string[][]>([
                ["", "", ""], 
                [
                    searchResponse.Timestamp.ToString("yyyyMMdd000000"),
                    searchResponse.MimeType,
                    searchResponse.Status
                ]]);
        }
        
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-1&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond("application/json", response);

        return (mockedRequest, searchResponse);
    }
    
    public static MockedRequest MockGetAvailableRequestFails(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, HttpStatusCode httpStatusCode)
    {
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-1&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond(httpStatusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockGetAvailableRequestNullResponse(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp)
    {
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-1&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond("application/json", JsonSerializer.Serialize<SearchResponse>(null!));

        return mockedRequest;
    }
}