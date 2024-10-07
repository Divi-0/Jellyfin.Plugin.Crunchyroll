using System.Net;
using System.Text.Json;
using AutoFixture;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using RichardSzalay.MockHttp;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.WaybackMachine.Helper;

public static class MockHttpResponse
{
    public static (MockedRequest mockedRequest, List<SearchResponse> response) MockSearchRequest(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, string response = "")
    {
        var fixture = new Fixture();
        
        var searchResponses = fixture.CreateMany<SearchResponse>().ToList();

        if (string.IsNullOrWhiteSpace(response))
        {
            response = JsonSerializer.Serialize<string[][]>([
                ["", "", ""], 
                [
                    searchResponses[0].Timestamp.ToString("yyyyMMddHHmmss"),
                    searchResponses[0].MimeType,
                    searchResponses[0].Status
                ],
                [
                    searchResponses[1].Timestamp.ToString("yyyyMMddHHmmss"),
                    searchResponses[1].MimeType,
                    searchResponses[1].Status
                ],
                [
                    searchResponses[2].Timestamp.ToString("yyyyMMddHHmmss"),
                    searchResponses[2].MimeType,
                    searchResponses[2].Status
                ],
            ]);
        }
        
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-3&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond("application/json", response);

        return (mockedRequest, searchResponses);
    }
    
    public static MockedRequest MockSearchRequestThrows(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, Exception exception)
    {
        var fixture = new Fixture();
        
        
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-3&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Throw(exception);

        return mockedRequest;
    }
    
    public static MockedRequest MockGetAvailableRequestFails(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp, HttpStatusCode httpStatusCode)
    {
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-3&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond(httpStatusCode);

        return mockedRequest;
    }
    
    public static MockedRequest MockGetAvailableRequestNullResponse(this MockHttpMessageHandler mockHttpMessageHandler, string url, 
        DateTime timeStamp)
    {
        var fullUrl = $"http://web.archive.org/cdx/search/cdx?url={url}&output=json&limit=-3&to={timeStamp.ToString("yyyyMMdd000000")}&fastLatest=true&fl=timestamp,mimetype,statuscode";
        var mockedRequest = mockHttpMessageHandler
            .When(fullUrl)
            .Respond("application/json", JsonSerializer.Serialize<SearchResponse>(null!));

        return mockedRequest;
    }
}