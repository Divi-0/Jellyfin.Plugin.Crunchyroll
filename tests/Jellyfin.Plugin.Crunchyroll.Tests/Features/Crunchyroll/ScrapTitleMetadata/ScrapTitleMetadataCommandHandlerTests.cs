using System.Globalization;
using System.Text.Json;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class ScrapTitleMetadataCommandHandlerTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    private readonly ScrapTitleMetadataCommandHandler _sut;
    
    private readonly IScrapTitleMetadataRepository _repository;
    private readonly ICrunchyrollSeasonsClient _crunchyrollSeasonsClient;
    private readonly ICrunchyrollEpisodesClient _crunchyrollEpisodesClient;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;

    public ScrapTitleMetadataCommandHandlerTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
        
        _repository = Substitute.For<IScrapTitleMetadataRepository>();
        _crunchyrollSeasonsClient = Substitute.For<ICrunchyrollSeasonsClient>();
        _crunchyrollEpisodesClient = Substitute.For<ICrunchyrollEpisodesClient>();
        _loginService = Substitute.For<ILoginService>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _sut = new ScrapTitleMetadataCommandHandler(_repository, _crunchyrollSeasonsClient,
            _crunchyrollEpisodesClient, _loginService, _crunchyrollSeriesClient);
    }

    [Fact]
    public async Task ReturnsSuccessAndStoresMetadata_WhenCalled_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);

        var rating = 3.5f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        Domain.Entities.TitleMetadata actualTitleMetadata = null!;
         _repository
             .AddOrUpdateTitleMetadata(Arg.Do<Domain.Entities.TitleMetadata>(
                    x => actualTitleMetadata = x),
                Arg.Any<CancellationToken>())
             .Returns(Result.Ok());

         _repository
             .SaveChangesAsync(Arg.Any<CancellationToken>())
             .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US")};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = rating,
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterTall.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterTall.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterTall.First().Last().Height
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterWide.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterWide.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterWide.First().Last().Height
            }),
            Language = language.Name
        };
        
        actualTitleMetadata.Should().BeEquivalentTo(expectedTitleMetadata, opt => opt
            .Excluding(x => x.Id)
            .Excluding(x => x.Seasons));

        actualTitleMetadata.Id.Should().NotBeEmpty();
        
        actualTitleMetadata.Seasons.Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.CrunchyrollId);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                var thumbnail = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
                thumbnail.Uri.Should().NotBeNullOrEmpty();
                thumbnail.Width.Should().NotBe(0);
                thumbnail.Height.Should().NotBe(0);
            });
        });
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task StoresEmptyEpisodeList_WhenEpisodeRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));
        
        var error = _fixture.Create<string>();
        _crunchyrollEpisodesClient
            .GetEpisodesAsync(seasonsResponse.Data.First().Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        var seriesContentItem = _fixture.Create<CrunchyrollSeriesContentItem>();
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesContentItem);
        
        var rating = 0.1f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);

        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US") };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Domain.Entities.TitleMetadata>(x => 
                x.CrunchyrollId == titleId &&
                x.SlugTitle == seriesContentItem.SlugTitle &&
                x.Seasons.All(season => seasonsResponse.Data.Any(y => y.Id == season.CrunchyrollId)) &&
                x.Seasons.Count(season => season.Episodes.Count == 0) == 1),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ScrapsExtraEpisodeIdForMovie_WhenMovieEpisodeIdNotPresentInMetadata_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var extraEpisodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 4.7f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        Domain.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x =>
                    actualTitleMetadata = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = extraEpisodeId,
            Title = _fixture.Create<string>(),
            Description = _fixture.Create<string>(),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand
        {
            TitleId = titleId, 
            MovieEpisodeId = extraEpisodeId,
            MovieSeasonId = seasonsResponse.Data[0].Id,
            Language = language
        };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = rating,
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterTall.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterTall.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterTall.First().Last().Height
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterWide.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterWide.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterWide.First().Last().Height
            }),
            Language = language.Name
        };
        
        actualTitleMetadata.Should().BeEquivalentTo(expectedTitleMetadata, opt => opt
            .Excluding(x => x.Id)
            .Excluding(x => x.Seasons));

        actualTitleMetadata.Id.Should().NotBeEmpty();
        
        actualTitleMetadata.Seasons.Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.CrunchyrollId);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                var thumbnail = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
                thumbnail.Uri.Should().NotBeNullOrEmpty();
                thumbnail.Width.Should().NotBe(0);
                thumbnail.Height.Should().NotBe(0);
            });
        });
        
        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualTitleMetadata.Seasons[0].Episodes.Should().Contain(x => x.CrunchyrollId == extraEpisodeId);
    }
    
    [Fact]
    public async Task ScrapsExtraEpisodeIdForMovie_WhenMovieEpisodeIdNotPresentInMetadataAndSeasonAlsoNotFound_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var extraEpisodeId = CrunchyrollIdFaker.Generate();
        var extraSeasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 5.0f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        Domain.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x =>
                    actualTitleMetadata = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = extraEpisodeId,
            Title = _fixture.Create<string>(),
            Description = _fixture.Create<string>(),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = extraSeasonId,
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand
        {
            TitleId = titleId, 
            MovieEpisodeId = extraEpisodeId,
            MovieSeasonId = extraSeasonId,
            Language = language
        };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = rating,
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterTall.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterTall.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterTall.First().Last().Height
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterWide.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterWide.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterWide.First().Last().Height
            }),
            Language = language.Name
        };
        
        actualTitleMetadata.Should().BeEquivalentTo(expectedTitleMetadata, opt => opt
            .Excluding(x => x.Id)
            .Excluding(x => x.Seasons));
        
        actualTitleMetadata.Id.Should().NotBeEmpty();
        
        actualTitleMetadata.Seasons[..^1].Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.CrunchyrollId);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                var thumbnail = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
                thumbnail.Uri.Should().NotBeNullOrEmpty();
                thumbnail.Width.Should().NotBe(0);
                thumbnail.Height.Should().NotBe(0);
            });
        });
        
        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualTitleMetadata.Seasons
            .First(x => x.CrunchyrollId == extraSeasonId).Episodes
            .Should().Contain(x => x.CrunchyrollId == extraEpisodeId);
    }
    
    [Fact]
    public async Task ScrapsNotExtraEpisodeIdForMovie_WhenMovieEpisodeIdAlreadyPresentInMetadata_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var extraEpisodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany(3)
            .ToList();
        
        titleMetadata.Seasons.AddRange(seasons);
        
        titleMetadata.Seasons.Last().Episodes.Add(new Episode
        {
            CrunchyrollId = extraEpisodeId,
            Description = "abc",
            Title = "gfe",
            EpisodeNumber = string.Empty,
            SequenceNumber = 0,
            SlugTitle = string.Empty,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = "abfe",
                Height = 123,
                Width = 432
            }),
            SeasonId = titleMetadata.Seasons.Last().Id,
            Language = language.Name
        });
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var crunchyrollSeasonsItem = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItem.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.CrunchyrollId)
                .Create());
        }
        
        var seasonsResponse = _fixture
            .Build<CrunchyrollSeasonsResponse>()
            .With(x => x.Data, crunchyrollSeasonsItem)
            .Create();
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        var newSeasonEpisodes = new Dictionary<string, IReadOnlyList<CrunchyrollEpisodeItem>>();
        foreach (var season in seasonsResponse.Data)
        {
            var episodes = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodes);
            
            newSeasonEpisodes.Add(season.Id, episodes.Data);
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        const float rating = 5.0f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Domain.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x => actualMetadata = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand
        {
            TitleId = titleId, 
            MovieSeasonId = titleMetadata.Seasons.Last().CrunchyrollId,
            MovieEpisodeId = extraEpisodeId,
            Language = language
        };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualMetadata.CrunchyrollId.Should().Be(titleMetadata.CrunchyrollId);
        actualMetadata.SlugTitle.Should().Be(titleMetadata.SlugTitle);
        actualMetadata.Seasons.Should().AllSatisfy(currentSeason =>
        {
            titleMetadata.Seasons.Should().Contain(season => season.Id == currentSeason.Id);

            var oldEpisodes = titleMetadata.Seasons.First(oldSeason => oldSeason.Id == currentSeason.Id).Episodes;
            oldEpisodes.All(epsiode => currentSeason.Episodes.Contains(epsiode)).Should().BeTrue();
            
            newSeasonEpisodes[currentSeason.CrunchyrollId].All(newEpisodes =>
                currentSeason.Episodes.Any(actualEpisode => actualEpisode.CrunchyrollId == newEpisodes.Id))
                .Should().BeTrue();
        });
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualMetadata.Seasons.Should().Contain(x => x.Episodes.Any(y => y.CrunchyrollId == extraEpisodeId));
    }
    
    [Fact]
    public async Task DoesNotConatinExtraEpisodeIdForMovie_WhenGetEpisodeForMovieEpisodeIdFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var extraEpisodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 2.9f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        Domain.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x =>
                    actualTitleMetadata = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand
        {
            TitleId = titleId, 
            MovieEpisodeId = extraEpisodeId,
            MovieSeasonId = seasonsResponse.Data.First().Id,
            Language = new CultureInfo("en-US")
        };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = rating,
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterTall.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterTall.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterTall.First().Last().Height
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterWide.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterWide.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterWide.First().Last().Height
            }),
            Language = language.Name
        };
        
        actualTitleMetadata.Should().BeEquivalentTo(expectedTitleMetadata, opt => opt
            .Excluding(x => x.Id)
            .Excluding(x => x.Seasons));

        actualTitleMetadata.Id.Should().NotBeEmpty();
        
        actualTitleMetadata.Seasons.Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.CrunchyrollId);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                var thumbnail = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
                thumbnail.Uri.Should().NotBeNullOrEmpty();
                thumbnail.Width.Should().NotBe(0);
                thumbnail.Height.Should().NotBe(0);
            });
        });
        
        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualTitleMetadata.Seasons.Should().NotContain(x => x.Episodes.Any(y => y.CrunchyrollId == extraEpisodeId));
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryAddOrUpdateFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 2.5f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<Domain.Entities.TitleMetadata?>(null)));
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US")};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositorySaveChangesFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<Domain.Entities.TitleMetadata?>(null)));
        
        var rating = 1.2f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US")};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}