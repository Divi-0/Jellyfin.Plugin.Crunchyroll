using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class ScrapTitleMetadataCommandHandlerTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    private readonly ScrapTitleMetadataCommandHandler _sut;
    
    private readonly IScrapTitleMetadataSession _scrapTitleMetadataSession;
    private readonly ICrunchyrollSeasonsClient _crunchyrollSeasonsClient;
    private readonly ICrunchyrollEpisodesClient _crunchyrollEpisodesClient;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;

    public ScrapTitleMetadataCommandHandlerTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
        
        _scrapTitleMetadataSession = Substitute.For<IScrapTitleMetadataSession>();
        _crunchyrollSeasonsClient = Substitute.For<ICrunchyrollSeasonsClient>();
        _crunchyrollEpisodesClient = Substitute.For<ICrunchyrollEpisodesClient>();
        _loginService = Substitute.For<ILoginService>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _sut = new ScrapTitleMetadataCommandHandler(_scrapTitleMetadataSession, _crunchyrollSeasonsClient,
            _crunchyrollEpisodesClient, _loginService, _crunchyrollSeriesClient);
    }

    [Fact]
    public async Task ReturnsSuccessAndStoresMetadata_WhenCalled_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
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
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(ValueTask.FromResult<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));

        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualTitleMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(
                Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x =>
                    actualTitleMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        actualTitleMetadata.Should().NotBeNull();
        actualTitleMetadata.TitleId.Should().Be(titleId);
        actualTitleMetadata.Title.Should().Be(seriesMetadataResponse.Title);
        actualTitleMetadata.Description.Should().Be(seriesMetadataResponse.Description);
        actualTitleMetadata.SlugTitle.Should().Be(seriesMetadataResponse.SlugTitle);
        actualTitleMetadata.Studio.Should().Be(seriesMetadataResponse.ContentProvider);
        actualTitleMetadata.PosterTallUri.Should().Be(seriesMetadataResponse.Images.PosterTall.First().Last().Source);
        actualTitleMetadata.PosterWideUri.Should().Be(seriesMetadataResponse.Images.PosterWide.First().Last().Source);
        actualTitleMetadata.Seasons.Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.Id);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                episode.ThumbnailUrl.Should().NotBeNullOrEmpty();
            });
        });
    }

    [Fact]
    public async Task ForwardsError_WhenSeasonsRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = _fixture.Create<string>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, CancellationToken.None)
            .Returns(Result.Fail(error));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());

        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, CancellationToken.None);
    }

    [Fact]
    public async Task ForwardsError_WhenSeriesMetadataRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var error = Guid.NewGuid().ToString();
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());

        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, CancellationToken.None);
        
        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, CancellationToken.None);

        await _scrapTitleMetadataSession
            .DidNotReceive()
            .AddOrUpdateTitleMetadata(Arg
                .Any<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>());
    }
    
    [Fact]
    public async Task StoresEmptyEpisodeList_WhenEpisodeRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }
        
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(ValueTask.FromResult((Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?)null));
        
        var error = _fixture.Create<string>();
        _crunchyrollEpisodesClient
            .GetEpisodesAsync(seasonsResponse.Data.First().Id, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        var seriesContentItem = _fixture.Create<CrunchyrollSeriesContentItem>();
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(seriesContentItem);
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());
        
        await _scrapTitleMetadataSession
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
                x.TitleId == titleId &&
                x.SlugTitle == seriesContentItem.SlugTitle &&
                x.Seasons.All(season => seasonsResponse.Data.Any(y => y.Id == season.Id)) &&
                x.Seasons.Count(season => season.Episodes.Count == 0) == 1));
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenLoginFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        var error = _fixture.Create<string>();
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotOverrideExistingEpisodes_WhenEpisodesRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);

        var crunchyrollSeasonsItem = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItem.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.Id)
                .Create());
        }
        
        var seasonsResponse = _fixture
            .Build<CrunchyrollSeasonsResponse>()
            .With(x => x.Data, crunchyrollSeasonsItem)
            .Create();
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(Result.Fail(_fixture.Create<string>()));
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
                x.TitleId == titleMetadata.TitleId &&
                x.SlugTitle == titleMetadata.SlugTitle &&
                x.Seasons.All(season => titleMetadata.Seasons.Any(y => y.Id == season.Id)) &&
                x.Seasons.All(season => season.Episodes.Any())));
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerEpisodesToEpisodesList_WhenCompletelyNewEpisodesWereListedAndOldRemoved_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);

        var crunchyrollSeasonsItem = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItem.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.Id)
                .Create());
        }
        
        var seasonsResponse = _fixture
            .Build<CrunchyrollSeasonsResponse>()
            .With(x => x.Data, crunchyrollSeasonsItem)
            .Create();
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        var newSeasonEpisodes = new Dictionary<string, IReadOnlyList<CrunchyrollEpisodeItem>>();
        foreach (var season in seasonsResponse.Data)
        {
            var episodes = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(episodes);
            
            newSeasonEpisodes.Add(season.Id, episodes.Data);
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);

        actualMetadata.TitleId.Should().Be(titleMetadata.TitleId);
        actualMetadata.SlugTitle.Should().Be(titleMetadata.SlugTitle);
        actualMetadata.Seasons.Should().AllSatisfy(currentSeason =>
        {
            titleMetadata.Seasons.Should().Contain(season => season.Id == currentSeason.Id);

            var oldEpisodes = titleMetadata.Seasons.First(oldSeason => oldSeason.Id == currentSeason.Id).Episodes;
            oldEpisodes.All(epsiode => currentSeason.Episodes.Contains(epsiode)).Should().BeTrue();
            
            newSeasonEpisodes[currentSeason.Id].All(newEpisodes =>
                currentSeason.Episodes.Any(actualEpisode => actualEpisode.Id == newEpisodes.Id))
                .Should().BeTrue();
        });
    }
    
    [Fact]
    public async Task ReturnsSuccessAndUpdateMetadataOfSeries_WhenNewMetadataForSeriesIsAvailable_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);

        var crunchyrollSeasonsItem = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItem.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.Id)
                .Create());
        }
        
        var seasonsResponse = _fixture
            .Build<CrunchyrollSeasonsResponse>()
            .With(x => x.Data, crunchyrollSeasonsItem)
            .Create();
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodes = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
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
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);

        actualMetadata.TitleId.Should().Be(titleMetadata.TitleId);
        actualMetadata.SlugTitle.Should().Be(titleMetadata.SlugTitle);
        actualMetadata.Title.Should().Be(seriesMetadataResponse.Title);
        actualMetadata.Description.Should().Be(seriesMetadataResponse.Description);
        actualMetadata.Studio.Should().Be(seriesMetadataResponse.ContentProvider);
        actualMetadata.PosterTallUri.Should().Be(seriesMetadataResponse.Images.PosterTall.First().Last().Source);
        actualMetadata.PosterWideUri.Should().Be(seriesMetadataResponse.Images.PosterWide.First().Last().Source);
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerEpisodesToEpisodesList_WhenNewEpisodesWereListed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);

        var crunchyrollSeasonsItem = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItem.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.Id)
                .Create());
        }
        
        var seasonsResponse = _fixture
            .Build<CrunchyrollSeasonsResponse>()
            .With(x => x.Data, crunchyrollSeasonsItem)
            .Create();
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
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
                .With(x => x.Id, titleMetadata.Seasons.First().Episodes.First().Id)
                .Create();

            episodes.Add(existingEpisode);
            
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(new CrunchyrollEpisodesResponse{Data = episodes});
            
            newSeasonEpisodes.Add(season.Id, episodes);
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }
        
        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);

        actualMetadata.TitleId.Should().Be(titleMetadata.TitleId);
        actualMetadata.SlugTitle.Should().Be(titleMetadata.SlugTitle);
        actualMetadata.Seasons.Should().AllSatisfy(currentSeason =>
        {
            titleMetadata.Seasons.Should().Contain(season => season.Id == currentSeason.Id);

            var oldEpisodes = titleMetadata.Seasons.First(oldSeason => oldSeason.Id == currentSeason.Id).Episodes;
            oldEpisodes.All(epsiode => currentSeason.Episodes.Contains(epsiode)).Should().BeTrue();
            
            newSeasonEpisodes[currentSeason.Id].All(newEpisodes =>
                currentSeason.Episodes.Any(actualEpisode => actualEpisode.Id == newEpisodes.Id))
                .Should().BeTrue();
        });
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerSeasonsToSeasonsList_WhenNewSeasonsWereListed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);

        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItems.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.Id)
                .Create());
        }

        var newSeason = _fixture.Create<CrunchyrollSeasonsItem>();
        crunchyrollSeasonsItems.Add(newSeason);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems});
        
        foreach (var season in crunchyrollSeasonsItems)
        {
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(_fixture.Create<CrunchyrollEpisodesResponse>());
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in crunchyrollSeasonsItems)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);

        actualMetadata.Seasons.Should().HaveCount(4);

        foreach (var seasonsItem in crunchyrollSeasonsItems)
        {
            actualMetadata.Seasons.Should().Contain(x => x.Id == seasonsItem.Id);
        }
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotDeleteSeasons_WhenOldSeasonsWereRemoved_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);
        
        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItems.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.Id)
                .Create());
        }
        
        var removedSeason = crunchyrollSeasonsItems.First();
        crunchyrollSeasonsItems.Remove(removedSeason);

        var newSeason = _fixture.Create<CrunchyrollSeasonsItem>();
        crunchyrollSeasonsItems.Add(newSeason);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems});
        
        foreach (var season in crunchyrollSeasonsItems)
        {
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>())
                .Returns(_fixture.Create<CrunchyrollEpisodesResponse>());
        }
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession.AddOrUpdateTitleMetadata(
            Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        foreach (var season in crunchyrollSeasonsItems)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);
        
        actualMetadata.Seasons.Should().Contain(x => x.Id == newSeason.Id, "new season needs to be added");
        actualMetadata.Seasons.Should().Contain(titleMetadata.Seasons, "the removed season should still be in the list");
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotDeleteSeasons_WhenOldSeasonsWereRemovedAndNewAdded_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadataAsync(titleId)
            .Returns(titleMetadata);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = Array.Empty<CrunchyrollSeasonsItem>()});
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CrunchyrollSeriesContentItem>());
        
        Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession.AddOrUpdateTitleMetadata(
            Arg.Do<Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadataAsync(titleId);

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CancellationToken>());

        actualMetadata.Seasons.Should().BeEquivalentTo(titleMetadata.Seasons);
    }
}