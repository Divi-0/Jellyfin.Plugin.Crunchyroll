using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using AutoFixture;
using Bogus;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using Microsoft.Net.Http.Headers;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;

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
        CrunchyrollId seriesId, SeriesInfo seriesInfo, Action<CrunchyrollSearchResponse>? options = null)
    {
        var fixture = new Fixture();

        var title = Path.GetFileNameWithoutExtension(seriesInfo.Path);

        var searchDataItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ =>
            {
                var randomTitle = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                return new CrunchyrollSearchDataItem
                {
                    Id = CrunchyrollIdFaker.Generate(),
                    Title = randomTitle,
                    SlugTitle = CrunchyrollSlugFaker.Generate(randomTitle)
                };
            })
            .ToList();

        var searchDataItem = new CrunchyrollSearchDataItem
        {
            Id = seriesId,
            Title = title,
            SlugTitle = CrunchyrollSlugFaker.Generate(title)
        };
        
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
                                Pattern = title
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
                                Pattern = seriesInfo.GetPreferredMetadataCultureInfo().Name
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
    
    public static async Task<CrunchyrollSearchResponse> MockCrunchyrollSearchResponseForMovie(this IWireMockAdminApi wireMockAdminApi, 
        string title, string language, string episodeId, string seasonId, string seriesId)
    {
        var fixture = new Fixture();

        var searchDataItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ =>
            {
                var randomTitle = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                return new CrunchyrollSearchDataItem
                {
                    Id = CrunchyrollIdFaker.Generate(),
                    Title = randomTitle,
                    SlugTitle = CrunchyrollSlugFaker.Generate(randomTitle)
                };
            })
            .ToList();

        var searchDataItem = new CrunchyrollSearchDataItem
        {
            Id = episodeId,
            Title = title,
            SlugTitle = CrunchyrollSlugFaker.Generate(title),
            EpisodeMetadata = new CrunchyrollSearchDataEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = seasonId,
                SequenceNumber = 0,
                SeriesId = seriesId,
                SeriesSlugTitle = "bla-bla"
            }
        };
        
        searchDataItems.Add(searchDataItem);
        
        var searchDatas = fixture.Build<CrunchyrollSearchData>()
            .CreateMany().ToList();

        var searchData = fixture.Build<CrunchyrollSearchData>()
            .With(x => x.Type, "episodes")
            .With(x => x.Items, searchDataItems)
            .Create();
        
        searchDatas.Add(searchData);
        
        var searchResponse = fixture.Build<CrunchyrollSearchResponse>()
            .With(x => x.Data, searchDatas)
            .Create();
        
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
                                Pattern = title
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
                                Name = "WildcardMatcher",
                                Pattern = "episode"
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
    
    public static async Task<IReadOnlyList<SearchResponse>> MockWaybackMachineSearchResponse(this IWireMockAdminApi wireMockAdminApi, 
        string crunchyrollUrl)
    {
        var fixture = new Fixture()
            .Customize(new WaybackMachineSearchResponseCustomization());

        var searchResponses = fixture.CreateMany<SearchResponse>().ToList();
        string[][] response = [
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
        ];
        
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
                                    Pattern = "-5"
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
                        },
                        new ParamModel()
                        {
                            Name = "filter",
                            Matchers = new MatcherModel[]
                            {
                                new MatcherModel()
                                {
                                    Name = "WildcardMatcher",
                                    Pattern = "statuscode:200"
                                }
                            }
                        }
                    }))
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(response))
            ));

        await builder.BuildAndPostAsync();

        return searchResponses;
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
                    .Replace("http://web.archive.org", mockedArchiveOrgUrl)
                    .Replace("https://web.archive.org", mockedArchiveOrgUrl))
            ));

        await builder.BuildAndPostAsync();
    }
    
    public static async Task MockWaybackMachineArchivedUrlWithCrunchyrollCommentsHtml(this IWireMockAdminApi wireMockAdminApi, 
        string url, string mockedArchiveOrgUrl)
    {
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithUrl(url))
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(Properties.Resources.CrunchyrollEpisodeHtml
                    .Replace("http://web.archive.org", mockedArchiveOrgUrl)
                    .Replace("https://web.archive.org", mockedArchiveOrgUrl))
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
    
    public static async Task<CrunchyrollSeasonsResponse> MockCrunchyrollSeasonsResponse(this IWireMockAdminApi wireMockAdminApi, 
        List<SeasonInfo> seasons, string seriesId, string language)
    {
        var seasonsResponse = new CrunchyrollSeasonsResponse()
        {
            Data = seasons
                .Select(season =>
                {
                    var title = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                    return new CrunchyrollSeasonsItem()
                    {
                        Id = season.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeasonId) ?? CrunchyrollIdFaker.Generate(),
                        Title = title,
                        SlugTitle = CrunchyrollSlugFaker.Generate(title),
                        SeasonNumber = season.IndexNumber!.Value,
                        SeasonSequenceNumber = season.IndexNumber!.Value,
                        Identifier = $"{seriesId}|S{season.IndexNumber!.Value}",
                        SeasonDisplayNumber = season.IndexNumber!.Value.ToString()
                    };
                })
                .ToList()
        };
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/cms/series/{seriesId}/seasons")
                .WithParams(new List<ParamModel>()
                {
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
                    }
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
                .WithBody(JsonSerializer.Serialize(seasonsResponse))
            ));

        await builder.BuildAndPostAsync();

        return seasonsResponse;
    }
    
    public static async Task<CrunchyrollSeasonsResponse> MockCrunchyrollSeasonsResponseWithDuplicate(this IWireMockAdminApi wireMockAdminApi, 
        string titleId, string language, int duplicateSeasonNumber)
    {
        var fixture = new Fixture();
        
        var duplicateSeasons = fixture
            .Build<CrunchyrollSeasonsItem>()
            .With(x => x.SeasonNumber, duplicateSeasonNumber)
            .CreateMany(2);

        var seasons = fixture
            .CreateMany<CrunchyrollSeasonsItem>()
            .ToList();
        
        seasons.AddRange(duplicateSeasons);

        var seasonsResponse = new CrunchyrollSeasonsResponse()
        {
            Data = seasons
        };
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/cms/series/{titleId}/seasons")
                .WithParams(new List<ParamModel>()
                {
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
                    }
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
                .WithBody(JsonSerializer.Serialize(seasonsResponse))
            ));

        await builder.BuildAndPostAsync();

        return seasonsResponse;
    }
    
    public static async Task<CrunchyrollEpisodesResponse> MockCrunchyrollEpisodesResponse(this IWireMockAdminApi wireMockAdminApi, 
        List<EpisodeInfo> episodes, string seasonId, string language, string crunchyrollUrl, CrunchyrollEpisodesResponse? response = null)
    {
        var faker = new Faker();

        var episodesResponse = response ?? new CrunchyrollEpisodesResponse
        {
            Data = episodes.Select(episode =>
            {
                var title = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                var item = new CrunchyrollEpisodeItem
                {
                    Id = CrunchyrollIdFaker.Generate(),
                    Title = title,
                    SlugTitle = CrunchyrollSlugFaker.Generate(title),
                    Description = new Faker().Lorem.Sentences(),
                    Episode = episode.IndexNumber!.Value.ToString(),
                    EpisodeNumber = episode.IndexNumber!.Value,
                    Images = new CrunchyrollEpisodeImages
                    {
                        Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes
                        {
                            Source = faker.Internet.UrlWithPath(Uri.UriSchemeHttp, domain: crunchyrollUrl, fileExt: "jpg"),
                            Type = "thumbnail",
                            Height = 0,
                            Width = 0
                        }]]
                    },
                    SequenceNumber = episode.IndexNumber!.Value,
                    SeasonId = CrunchyrollIdFaker.Generate(),
                    SeriesId = CrunchyrollIdFaker.Generate(),
                    SeriesSlugTitle= CrunchyrollSlugFaker.Generate()
                };
                return item;
            }).ToList()
        };
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/cms/seasons/{seasonId}/episodes")
                .WithParams(new List<ParamModel>()
                {
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
                    }
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
                .WithBody(JsonSerializer.Serialize(episodesResponse))
            ));

        await builder.BuildAndPostAsync();

        return episodesResponse;
    }
    
    public static async Task<CrunchyrollEpisodeResponse> MockCrunchyrollGetEpisodeResponse(this IWireMockAdminApi wireMockAdminApi, 
        string episodeId, string seasonId, string seriesId, string language, string crunchyrollUrl)
    {
        var faker = new Faker();

        var title = $"{faker.Random.Words()}-{Random.Shared.Next(9999)}";
        var episodesResponse = new CrunchyrollEpisodeResponse
        {
            Data = [
                new CrunchyrollEpisodeDataItem 
                {
                    Id = episodeId,
                    Title = title,
                    SlugTitle = CrunchyrollSlugFaker.Generate(title),
                    Description = new Faker().Lorem.Sentences(),
                    Images = new CrunchyrollEpisodeImages
                    {
                        Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes
                        {
                            Source = faker.Internet.UrlWithPath(Uri.UriSchemeHttp, domain: crunchyrollUrl, fileExt: "jpg"),
                            Type = "thumbnail",
                            Height = 0,
                            Width = 0
                        }]]
                    },
                    EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
                    {
                        Episode = string.Empty,
                        EpisodeNumber = null,
                        SeasonId = seasonId,
                        SeriesId = seriesId,
                        SeriesSlugTitle = CrunchyrollSlugFaker.Generate(),
                        SequenceNumber = 0,
                        SeasonNumber = Random.Shared.Next(1, 10),
                        SeasonTitle = faker.Random.Words(3),
                        SeasonDisplayNumber = string.Empty,
                        SeasonSequenceNumber = 0
                    },
                }
            ]
        };
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/cms/objects/{episodeId}")
                .WithParams(new List<ParamModel>()
                {
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
                    }
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
                .WithBody(JsonSerializer.Serialize(episodesResponse))
            ));

        await builder.BuildAndPostAsync();

        return episodesResponse;
    }
    
    public static async Task MockCrunchyrollEpisodeThumbnailResponse(this IWireMockAdminApi wireMockAdminApi, 
        CrunchyrollEpisodeItem episode)
    {
        var faker = new Faker();
        var content = faker.Random.Bytes(9999);
        var builder = wireMockAdminApi.GetMappingBuilder();
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithUrl(episode.Images.Thumbnail.First().Last().Source))
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsBytes(content)));

        await builder.BuildAndPostAsync();
    }
    
    public static async Task<CrunchyrollSeriesContentItem> MockCrunchyrollSeriesResponse(this IWireMockAdminApi wireMockAdminApi, 
        string seriesId, string language, string mockedCrunchyrollUrl)
    {
        var faker = new Faker();

        var crunchyrollUrl =
            mockedCrunchyrollUrl.Last() == '/' ? mockedCrunchyrollUrl[..^1] : mockedCrunchyrollUrl;

        var titleId = seriesId;
        var fakeTitle = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = fakeTitle,
            SlugTitle = CrunchyrollSlugFaker.Generate(fakeTitle),
            Description = faker.Lorem.Sentences(),
            ContentProvider = faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = faker.Internet.UrlWithPath(Uri.UriSchemeHttp, domain: crunchyrollUrl, fileExt: "jpg"),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = faker.Internet.UrlWithPath(Uri.UriSchemeHttp, domain: crunchyrollUrl, fileExt: "jpg"),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = faker.Internet.UrlWithPath(Uri.UriSchemeHttp, domain: crunchyrollUrl, fileExt: "jpg"),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = faker.Internet.UrlWithPath(Uri.UriSchemeHttp, domain: crunchyrollUrl, fileExt: "jpg"),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content/v2/cms/series/{titleId}")
                .WithParams(new List<ParamModel>()
                {
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
                    }
                })
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(new CrunchyrollSeriesContentResponse{Data = [seriesMetadataResponse]}))
            ));

        await builder.BuildAndPostAsync();

        return seriesMetadataResponse;
    }
    
    public static async Task<CrunchyrollSeriesRatingResponse> MockCrunchyrollSeriesRatingResponse(this IWireMockAdminApi wireMockAdminApi, 
        string seriesId)
    {
        var faker = new Faker();
        var builder = wireMockAdminApi.GetMappingBuilder();

        var response = new CrunchyrollSeriesRatingResponse { Average = faker.Random.Float(max: 5).ToString("0.#", CultureInfo.InvariantCulture) };
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithPath($"/content-reviews/v2/rating/series/{seriesId}")
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody(JsonSerializer.Serialize(response))
            ));

        await builder.BuildAndPostAsync();

        return response;
    }
    
    public static async Task<byte[]> MockCrunchyrollImagePosterResponse(this IWireMockAdminApi wireMockAdminApi, 
        string url)
    {
        var faker = new Faker();
        var content = faker.Random.Bytes(9999);
        var builder = wireMockAdminApi.GetMappingBuilder();
        
        builder.Given(m => m
            .WithRequest(req => req
                .UsingGet()
                .WithUrl(url)
            )
            .WithResponse(rsp => rsp
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsBytes(content)
            ));

        await builder.BuildAndPostAsync();

        return content;
    }
}