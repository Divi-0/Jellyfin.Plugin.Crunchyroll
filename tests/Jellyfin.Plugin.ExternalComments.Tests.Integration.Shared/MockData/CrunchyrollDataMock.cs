using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId.Client;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class CrunchyrollDataMock
{
    public static CrunchyrollAuthResponse GetAuthResponseMock()
    {
        return new CrunchyrollAuthResponse()
        {
            AccessToken = "token-123",
            TokenType = "Bearer",
            Scope = "scope",
            ExpiresIn = 3600,
            Country = "de"
        };
    }
    
    public static CrunchyrollSearchResponse GetSearchResponseMock(string titleId = "", string title = "")
    {
        var fixture = new Fixture();

        var searchDataItems = fixture.Build<CrunchyrollSearchDataItem>()
            .CreateMany().ToList();
        
        var searchDataItem = fixture.Build<CrunchyrollSearchDataItem>()
            .With(x => x.Id, titleId)
            .With(x => x.Title, title)
            .Create();
        
        searchDataItems.Add(searchDataItem);
        
        var searchDatas = fixture.Build<CrunchyrollSearchData>()
            .CreateMany().ToList();

        var searchData = fixture.Build<CrunchyrollSearchData>()
            .With(x => x.Type, "series")
            .With(x => x.Items, searchDataItems)
            .Create();
        
        searchDatas.Add(searchData);
        
        return fixture.Build<CrunchyrollSearchResponse>()
            .With(x => x.Data, searchDatas)
            .Create();
    }
    
    public static CrunchyrollCommentsResponse GetCommentsResponseMock()
    {
        var fixture = new Fixture();

        return fixture.Create<CrunchyrollCommentsResponse>();
    }

    public static CrunchyrollAuthResponse MockCrunchyrollAuthResponse(this MockHttpMessageHandler mockHttpMessageHandler, Action<CrunchyrollAuthResponse>? options = null)
    {
        var fixture = new Fixture();

        var authResponse = new CrunchyrollAuthResponse()
        {
            AccessToken = fixture.Create<string>(),
            TokenType = "Bearer",
            Scope = "scope",
            ExpiresIn = Random.Shared.Next(3600, 7200),
            Country = "de"
        };
        
        options?.Invoke(authResponse);
        
        mockHttpMessageHandler.When($"https://www.crunchyroll.com/")
            .Respond(HttpStatusCode.OK);
        
        mockHttpMessageHandler.When($"https://www.crunchyroll.com/auth/v1/token")
            .Respond("application/json", JsonSerializer.Serialize(authResponse));

        return authResponse;
    }

    public static CrunchyrollSearchResponse MockCrunchyrollSearchResponse(this MockHttpMessageHandler mockHttpMessageHandler, 
        string title, string language, Action<CrunchyrollSearchResponse>? options = null)
    {
        var fixture = new Fixture();

        var searchDataItems = fixture.Build<CrunchyrollSearchDataItem>()
            .CreateMany().ToList();
        
        var searchDataItem = fixture.Build<CrunchyrollSearchDataItem>()
            .With(x => x.Title, title)
            .Create();
        
        searchDataItems.Add(searchDataItem);
        
        var searchDatas = fixture.Build<CrunchyrollSearchData>()
            .CreateMany().ToList();

        var searchData = fixture.Build<CrunchyrollSearchData>()
            .With(x => x.Type, "series")
            .With(x => x.Items, searchDataItems)
            .Create();
        
        searchDatas.Add(searchData);
        
        var searchResponse = fixture.Build<CrunchyrollSearchResponse>()
            .With(x => x.Data, searchDatas)
            .Create();
        
        options?.Invoke(searchResponse);
        
        mockHttpMessageHandler.When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(title)}&n=6&type=series,movie_listing&ratings=true&locale={language}")
            .Respond("application/json", JsonSerializer.Serialize(searchResponse));

        return searchResponse;
    }

    public static CrunchyrollSearchResponse MockCrunchyrollSearchResponseNoMatch(this MockHttpMessageHandler mockHttpMessageHandler, 
        string title, string language, Action<CrunchyrollSearchResponse>? options = null)
    {
        var fixture = new Fixture();

        var searchDataItems = fixture.Build<CrunchyrollSearchDataItem>()
            .CreateMany().ToList();
        
        var searchDataItem = fixture.Build<CrunchyrollSearchDataItem>()
            .Create();
        
        searchDataItems.Add(searchDataItem);
        
        var searchDatas = fixture.Build<CrunchyrollSearchData>()
            .CreateMany().ToList();

        var searchData = fixture.Build<CrunchyrollSearchData>()
            .With(x => x.Type, "series")
            .With(x => x.Items, searchDataItems)
            .Create();
        
        searchDatas.Add(searchData);
        
        var searchResponse = fixture.Build<CrunchyrollSearchResponse>()
            .With(x => x.Data, searchDatas)
            .Create();
        
        options?.Invoke(searchResponse);
        
        mockHttpMessageHandler.When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(title)}&n=6&type=series,movie_listing&ratings=true&locale={language}")
            .Respond("application/json", JsonSerializer.Serialize(searchResponse));

        return searchResponse;
    }
}