using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class ScrapTitleMetadataCommandHandlerTests
{
    private readonly Fixture _fixture;
    
    public readonly ScrapTitleMetadataCommandHandler _sut;
    
    private readonly IScrapTitleMetadataSession _scrapTitleMetadataSession;
    private readonly ICrunchyrollSeasonsClient _crunchyrollSeasonsClient;
    private readonly ICrunchyrollEpisodesClient _crunchyrollEpisodesClient;
    private readonly ILogger<ScrapTitleMetadataCommandHandler> _logger;
    private readonly ILoginService _loginService;

    public ScrapTitleMetadataCommandHandlerTests()
    {
        _fixture = new Fixture();
        
        _scrapTitleMetadataSession = Substitute.For<IScrapTitleMetadataSession>();
        _crunchyrollSeasonsClient = Substitute.For<ICrunchyrollSeasonsClient>();
        _crunchyrollEpisodesClient = Substitute.For<ICrunchyrollEpisodesClient>();
        _logger = Substitute.For<ILogger<ScrapTitleMetadataCommandHandler>>();
        _loginService = Substitute.For<ILoginService>();
        _sut = new ScrapTitleMetadataCommandHandler(_scrapTitleMetadataSession, _crunchyrollSeasonsClient,
            _crunchyrollEpisodesClient, _logger, _loginService);
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
        
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
            .Returns(ValueTask.FromResult<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?>(null));
        
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
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
                x.TitleId == titleId &&
                x.SlugTitle == slugTitle &&
                x.Seasons.All(season => seasonsResponse.Data.Any(y => y.Id == season.Id)) &&
                x.Seasons.All(season => season.Episodes.Any())));
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
            .GetTitleMetadata(titleId)
            .Returns(ValueTask.FromResult((Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata?)null));
        
        var error = _fixture.Create<string>();
        _crunchyrollEpisodesClient
            .GetEpisodesAsync(seasonsResponse.Data.First().Id, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
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
        
        await _scrapTitleMetadataSession
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
                x.TitleId == titleId &&
                x.SlugTitle == slugTitle &&
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
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
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
            .GetTitleMetadata(titleId);

        await _scrapTitleMetadataSession
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => 
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
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
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
        
        Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
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
            .GetTitleMetadata(titleId);

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
    public async Task ReturnsSuccessAndAddsNewerEpisodesToEpisodesList_WhenNewEpisodesWereListed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
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
        
        Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
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
            .GetTitleMetadata(titleId);

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
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
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
        
        Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession
            .AddOrUpdateTitleMetadata(Arg.Do<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
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

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadata(titleId);

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
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
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
        
        Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession.AddOrUpdateTitleMetadata(
            Arg.Do<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
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

        await _scrapTitleMetadataSession
            .Received(1)
            .GetTitleMetadata(titleId);
        
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
        
        var titleMetadata = _fixture.Create<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>();
        _scrapTitleMetadataSession
            .GetTitleMetadata(titleId)
            .Returns(titleMetadata);
        
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = Array.Empty<CrunchyrollSeasonsItem>()});
        
        Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata actualMetadata = null!;
        await _scrapTitleMetadataSession.AddOrUpdateTitleMetadata(
            Arg.Do<Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities.TitleMetadata>(x => actualMetadata = x));
        
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
            .GetTitleMetadata(titleId);

        actualMetadata.Seasons.Should().BeEquivalentTo(titleMetadata.Seasons);
    }
}