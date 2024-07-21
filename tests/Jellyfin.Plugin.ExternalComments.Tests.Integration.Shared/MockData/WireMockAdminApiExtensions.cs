using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId.Client;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Fixture;
using Microsoft.Net.Http.Headers;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class WireMockAdminApiExtensions
{
    public static async Task MockRootPageAsync(this IWireMockAdminApi wireMockAdminApi)
    {
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath("/")
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
            ));

        await builder.BuildAndPostAsync();
    }
    
    public static async Task MockAnonymousAuthAsync(this IWireMockAdminApi wireMockAdminApi)
    {
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingPost()
                .WithPath("/auth/v1/token")
                .WithHeaders(new List<HeaderModel>(){new HeaderModel()
                {
                    Name = HeaderNames.Authorization,
                    Matchers = new List<MatcherModel>()
                    {
                        new MatcherModel()
                        {
                            Name = "WildcardMatcher",
                            Pattern = "Basic Y3Jfd2ViOg=="
                        }
                    }
                }})
                .WithBody(new BodyModel()
                {
                    Matcher = new MatcherModel()
                    {
                        Name = "WildcardMatcher",
                        Pattern = "grant_type=client_id"
                    }
                })
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(new CrunchyrollAuthResponse
                {
                    AccessToken = Guid.NewGuid().ToString(),
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    Scope = "account content mp offline_access",
                    Country = "US"
                }))
            ));
        
        await builder.BuildAndPostAsync();
    }
    
    public static async Task<CrunchyrollReviewsResponse> MockReviewsResponseAsync(this IWireMockAdminApi wireMockAdminApi, string titleId, string locacle, int pageNumber, int pageSize)
    {
        var fixture = new Fixture()
            .Customize(new CrunchyrollReviewsResponseCustomization())
            .Customize(new CrunchyrollReviewsResponseReviewItemRatingCustomization());

        var crunchyrollResponse = fixture.Create<CrunchyrollReviewsResponse>();
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content-reviews/v2/{locacle}/review/series/{titleId}/list")
                .WithParams(new List<ParamModel>()
                {
                    new ParamModel()
                    {
                        Name = "page",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = pageNumber.ToString()
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "page_size",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = pageSize.ToString()
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "sort",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = "helpful"
                            }
                        }
                    },
                })
                .WithHeaders(new List<HeaderModel>(){new HeaderModel()
                {
                    Name = HeaderNames.Authorization,
                    Matchers = new List<MatcherModel>()
                    {
                        new MatcherModel()
                        {
                            Name = "RegexMatcher",
                            Pattern = "Bearer .*"
                        }
                    }
                }})
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(crunchyrollResponse))
            ));
        
        await builder.BuildAndPostAsync();
        
        return crunchyrollResponse;
    }
    
    public static async Task<CrunchyrollSearchResponse> MockCrunchyrollSearchResponse(this IWireMockAdminApi wireMockAdminApi, 
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
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/discover/search")
                .WithParams(new List<ParamModel>()
                {
                    new ParamModel()
                    {
                        Name = "q",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = UrlEncoder.Default.Encode(title)
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "n",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = "6"
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "type",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "RegexMatcher",
                                Pattern = ".*"
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "ratings",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = "true"
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "locale",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = language
                            }
                        }
                    },
                })
                .WithHeaders(new List<HeaderModel>(){new HeaderModel()
                {
                    Name = HeaderNames.Authorization,
                    Matchers = new List<MatcherModel>()
                    {
                        new MatcherModel()
                        {
                            Name = "RegexMatcher",
                            Pattern = "Bearer .*"
                        }
                    }
                }})
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(searchResponse))
            ));

        await builder.BuildAndPostAsync();

        return searchResponse;
    }
    
    public static async Task<CrunchyrollSearchResponse> MockCrunchyrollSearchResponseNoMatch(this IWireMockAdminApi wireMockAdminApi, 
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
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/discover/search")
                .WithParams(new List<ParamModel>()
                {
                    new ParamModel()
                    {
                        Name = "q",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = UrlEncoder.Default.Encode(title)
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "n",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = "6"
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "type",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "RegexMatcher",
                                Pattern = ".*"
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "ratings",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = "true"
                            }
                        }
                    },
                    new ParamModel()
                    {
                        Name = "locale",
                        Matchers = new MatcherModel[]
                        {
                            new MatcherModel()
                            {
                                Name = "WildcardMatcher",
                                Pattern = language
                            }
                        }
                    },
                })
                .WithHeaders(new List<HeaderModel>(){new HeaderModel()
                {
                    Name = HeaderNames.Authorization,
                    Matchers = new List<MatcherModel>()
                    {
                        new MatcherModel()
                        {
                            Name = "RegexMatcher",
                            Pattern = "Bearer .*"
                        }
                    }
                }})
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(searchResponse))
            ));

        await builder.BuildAndPostAsync();

        return searchResponse;
    }
    
    public static async Task<SearchResponse> MockWaybackMachineSearchResponse(this IWireMockAdminApi wireMockAdminApi, 
        string crunchyrollUrl)
    {
        var fixture = new Fixture()
            .Customize(new WaybackMachineSearchResponseCustomization());

        var searchResponse = fixture.Create<SearchResponse>();
        string[][] response = [
            ["", "", ""], 
            [
                searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
                searchResponse.MimeType,
                searchResponse.Status
            ]];
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/cdx/search/cdx")
                .WithParams(new List<ParamModel>()
                    {
                        new ParamModel()
                        {
                            Name = "url",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = crunchyrollUrl
                                }
                            }
                        },
                        new ParamModel()
                        {
                            Name = "output",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = "json"
                                }
                            }
                        },
                        new ParamModel()
                        {
                            Name = "limit",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = "-1"
                                }
                            }
                        },
                        new ParamModel()
                        {
                            Name = "to",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = new DateTime(2024, 07, 10).ToString("yyyyMMdd000000")
                                }
                            }
                        },
                        new ParamModel()
                        {
                            Name = "fastLatest",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = "true"
                                }
                            }
                        },
                        new ParamModel()
                        {
                            Name = "fl",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = "*"
                                }
                            }
                        }
                    }))
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(response))
            ));

        await builder.BuildAndPostAsync();

        return searchResponse;
    }
    
    public static async Task MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(this IWireMockAdminApi wireMockAdminApi, 
        string url, string mockedArchiveOrgUrl)
    {
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithUrl(url))
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(Properties.Resources.CrunchyrollTitleHtml
                    .Replace("http://web.archive.org", mockedArchiveOrgUrl))
            ));

        await builder.BuildAndPostAsync();
    }
    
    public static async Task MockAvatarUriRequest(this IWireMockAdminApi wireMockAdminApi, 
        string uri)
    {
        var fixture = new Fixture();
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithUrl(uri))
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(Encoding.Default.GetString(fixture.Create<byte[]>()))
            ));

        await builder.BuildAndPostAsync();
    }
}