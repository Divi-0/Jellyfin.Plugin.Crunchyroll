using System.Globalization;
using System.Text.Json;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
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
            .Returns(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualTitleMetadata = null!;
         _repository
             .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(
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

        var expectedTitleMetadata = new Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata()
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
    public async Task SetsTitleMetadataRatingToZero_WhenGetSeriesRatingFailed_GivenTitleId()
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
        
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualTitleMetadata = null!;
         _repository
             .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(
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

        var expectedTitleMetadata = new Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = 0,
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
    public async Task ReturnsFailed_WhenRepositoryGetTitleMetadataFails_GivenTitleId()
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
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = language};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadata(Arg.Any<Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForwardsError_WhenSeasonsRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = _fixture.Create<string>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), CancellationToken.None)
            .Returns(Result.Fail(error));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US") };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), CancellationToken.None);
    }

    [Fact]
    public async Task ForwardsError_WhenSeriesMetadataRequestFailed_GivenTitleId()
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
            .GetTitleMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        var error = Guid.NewGuid().ToString();
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US") };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), CancellationToken.None);
        
        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), CancellationToken.None);

        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadata(Arg.Any<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(),
                Arg.Any<CancellationToken>());
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
            .Returns(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));
        
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
                Arg.Any<Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(),
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
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
                x.CrunchyrollId == titleId &&
                x.SlugTitle == seriesContentItem.SlugTitle &&
                x.Seasons.All(season => seasonsResponse.Data.Any(y => y.Id == season.CrunchyrollId)) &&
                x.Seasons.Count(season => season.Episodes.Count == 0) == 1),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenLoginFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        var error = _fixture.Create<string>();
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US") };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotOverrideExistingEpisodes_WhenGetEpisodesRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany()
            .ToList();

        foreach (var season in seasons)
        {
            var episodes = _fixture.Build<Episode>()
                .Without(x => x.Season)
                .With(x => x.SeasonId, season.Id)
                .CreateMany()
                .ToList();
            
            season.Episodes.AddRange(episodes);
        }
        
        titleMetadata.Seasons.AddRange(seasons);
        
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
        
        foreach (var season in seasonsResponse.Data)
        {
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(Result.Fail(_fixture.Create<string>()));
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        const float rating = 4.1f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);

        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(),
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
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
                x.CrunchyrollId == titleMetadata.CrunchyrollId &&
                x.SlugTitle == titleMetadata.SlugTitle &&
                x.Seasons.All(season => titleMetadata.Seasons.Any(y => y.CrunchyrollId == season.CrunchyrollId)) &&
                x.Seasons.All(season => season.Episodes.Any())),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerEpisodesToEpisodesList_WhenCompletelyNewEpisodesWereListedAndOldRemoved_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();

        var seasons = _fixture.Build<Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany();
        
        titleMetadata.Seasons.AddRange(seasons);
        
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
        
        const float rating = 1.9f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
    }
    
    [Fact]
    public async Task ReturnsSuccessAndUpdateMetadataOfSeries_WhenNewMetadataForSeriesIsAvailable_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
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
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodes = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodes);
        }
        
        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem
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
        
        const float rating = 1.1f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
        actualMetadata.Title.Should().Be(seriesMetadataResponse.Title);
        actualMetadata.Description.Should().Be(seriesMetadataResponse.Description);
        actualMetadata.Studio.Should().Be(seriesMetadataResponse.ContentProvider);
        actualMetadata.Rating.Should().Be(rating);
        var posterTall = JsonSerializer.Deserialize<ImageSource>(actualMetadata.PosterTall)!;
        var posterWide = JsonSerializer.Deserialize<ImageSource>(actualMetadata.PosterWide)!;
        posterTall.Uri.Should().Be(seriesMetadataResponse.Images.PosterTall.First().Last().Source);
        posterWide.Uri.Should().Be(seriesMetadataResponse.Images.PosterWide.First().Last().Source);
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerEpisodesToEpisodesList_WhenNewEpisodesWereListed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany()
            .ToList();
        
        seasons.First().Episodes.Add(CrunchyrollEpisodeFaker.Generate(seasons.First().Id));
        
        titleMetadata.Seasons.AddRange(seasons);
        
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
            var episodes = _fixture
                .Build<CrunchyrollEpisodeItem>()
                .CreateMany()
                .ToList();

            var existingEpisode = _fixture
                .Build<CrunchyrollEpisodeItem>()
                .With(x => x.Id, titleMetadata.Seasons.First().Episodes.First().CrunchyrollId)
                .Create();

            episodes.Add(existingEpisode);
            
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(new CrunchyrollEpisodesResponse{Data = episodes});
            
            newSeasonEpisodes.Add(season.Id, episodes);
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        const float rating = 0.1f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerSeasonsToSeasonsList_WhenNewSeasonsWereListed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany(3)
            .ToList();
        
        titleMetadata.Seasons.AddRange(seasons);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItems.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.CrunchyrollId)
                .Create());
        }

        var newSeason = _fixture.Create<CrunchyrollSeasonsItem>();
        crunchyrollSeasonsItems.Add(newSeason);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems});
        
        foreach (var season in crunchyrollSeasonsItems)
        {
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(_fixture.Create<CrunchyrollEpisodesResponse>());
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        const float rating = 4.9f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in crunchyrollSeasonsItems)
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

        actualMetadata.Seasons.Should().HaveCount(4);

        foreach (var seasonsItem in crunchyrollSeasonsItems)
        {
            actualMetadata.Seasons.Should().Contain(x => x.CrunchyrollId == seasonsItem.Id);
        }
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotDeleteSeasons_WhenOldSeasonsWereRemoved_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany()
            .ToList();
        
        titleMetadata.Seasons.AddRange(seasons);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItems.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.CrunchyrollId)
                .Create());
        }
        
        var removedSeason = crunchyrollSeasonsItems.First();
        crunchyrollSeasonsItems.Remove(removedSeason);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems});
        
        foreach (var season in crunchyrollSeasonsItems)
        {
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(_fixture.Create<CrunchyrollEpisodesResponse>());
        }
        
        _crunchyrollEpisodesClient
            .GetEpisodesAsync(removedSeason.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollEpisodesResponse>());
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        const float rating = 3.3f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository.AddOrUpdateTitleMetadata(
            Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in crunchyrollSeasonsItems)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        actualMetadata.Seasons.Should().Contain(titleMetadata.Seasons, "the removed season should still be in the list");
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotDeleteSeasons_WhenOldSeasonsWereRemovedAndNewAdded_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        const float rating = 2.2f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = Array.Empty<CrunchyrollSeasonsItem>()});
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository.AddOrUpdateTitleMetadata(
            Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualMetadata.Seasons.Should().BeEquivalentTo(titleMetadata.Seasons);
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
            .Returns(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x =>
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

        var expectedTitleMetadata = new Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata()
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
            .Returns(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x =>
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

        var expectedTitleMetadata = new Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata()
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
        
        var titleMetadata = _fixture.Build<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>()
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
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x),
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
            .Returns(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x =>
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

        var expectedTitleMetadata = new Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata()
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
            .Returns(Task.FromResult(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null)));
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(), 
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
            .Returns(Task.FromResult(Result.Ok<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null)));
        
        var rating = 1.2f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(), 
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