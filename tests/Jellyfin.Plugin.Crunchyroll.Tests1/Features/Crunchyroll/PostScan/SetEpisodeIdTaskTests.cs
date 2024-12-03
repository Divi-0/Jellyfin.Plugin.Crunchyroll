using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.MediaInfo;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class SetEpisodeIdTaskTests
{
    private readonly SetEpisodeIdTask _sut;

    private readonly IMediator _mediator;
    private readonly IPostEpisodeIdSetTask[] _postSeasonIdSetTasks;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemRepository _itemRepository;

    public SetEpisodeIdTaskTests()
    {
        _postSeasonIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => Substitute.For<IPostEpisodeIdSetTask>())
            .ToArray();
            
        _mediator = Substitute.For<IMediator>();
        _libraryManager = MockHelper.LibraryManager;
        _itemRepository = MockHelper.ItemRepository;
        var logger = Substitute.For<ILogger<SetEpisodeIdTask>>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();

        scope.ServiceProvider
            .GetService<IEnumerable<IPostEpisodeIdSetTask>>()
            .Returns(_postSeasonIdSetTasks);
        
        scopeFactory
            .CreateScope()
            .Returns(scope);
            
        _sut = new SetEpisodeIdTask(_mediator, _libraryManager, logger, scopeFactory);
    }

    [Fact]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenSuccessful_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);
        
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episode.IndexNumber!.Value.ToString()))
            .Returns(new EpisodeIdResult(CrunchyrollIdFaker.Generate(), CrunchyrollSlugFaker.Generate()));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var episodeSlugTitle).Should().BeTrue();
        episodeId.Should().NotBeEmpty();
        episodeSlugTitle.Should().NotBeEmpty();
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received(1).RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
    
    [Theory]
    [InlineData("S01E1.5 - grfes", "1.5")]
    [InlineData("S06E15.5", "15.5")]
    [InlineData("S06E0032.9", "32.9")]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenEpisodeHasIndexNumberButNameIncludesDecimal_GivenSeasonWithSeasonId(
        string episodeName, string episodeIdentifier)
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        episode.Name = episodeName;
        episode.IndexNumber = Random.Shared.Next(1, int.MaxValue);
        episode.Path = $"/mnt/{new Faker().Random.Words()}/{episodeName}.mp4";
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var crunchyrollId = CrunchyrollIdFaker.Generate();
        var crunchyrollSlugTitle = CrunchyrollSlugFaker.Generate();
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episodeIdentifier))
            .Returns(new EpisodeIdResult(crunchyrollId, crunchyrollSlugTitle));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var episodeSlugTitle).Should().BeTrue();
        episodeId.Should().Be(crunchyrollId);
        episodeSlugTitle.Should().Be(crunchyrollSlugTitle);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received(1).RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
    
    [Theory]
    [InlineData("S03E13A", "13A")]
    [InlineData("S2E52b", "52b")]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenEpisodeHasIndexNumberButNameIncludesNumberWithLetter_GivenSeasonWithSeasonId(
        string episodeName, string episodeIdentifier)
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        episode.Name = episodeName;
        episode.IndexNumber = Random.Shared.Next(1, int.MaxValue);
        episode.Path = $"/mnt/{new Faker().Random.Words()}/{episodeName}.mp4";
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var crunchyrollId = CrunchyrollIdFaker.Generate();
        var crunchyrollSlugTitle = CrunchyrollSlugFaker.Generate();
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episodeIdentifier))
            .Returns(new EpisodeIdResult(crunchyrollId, crunchyrollSlugTitle));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var episodeSlugTitle).Should().BeTrue();
        episodeId.Should().Be(crunchyrollId);
        episodeSlugTitle.Should().Be(crunchyrollSlugTitle);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received(1).RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task IgnoresItem_WhenNoSeasonIdFound_GivenSeasonWithoutSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeFalse();
        episodeId.Should().BeNull();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.DidNotReceive().RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SkipItem_WhenEpisodeHasNoIndexNumberAndWasNotFoundByName_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        episode.IndexNumber = null;
        episode.Path = $"/mnt/123/{new Faker().Random.Words()}.mp4";
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        _mediator
            .Send(Arg.Any<EpisodeIdQueryByName>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeFalse();
        episodeId.Should().BeNull();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.DidNotReceive().RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SetsEmptyEpisodeId_WhenGetEpisodeByNameReturnsNull_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        episode.IndexNumber = null;
        episode.Path = $"/mnt/crunchyroll/{new Faker().Random.Words()}.mp4";
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);

        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        _mediator
            .Send(Arg.Any<EpisodeIdQueryByName>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<EpisodeIdResult?>(null));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episodeId.Should().BeEmpty();
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received().RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task SetsEpisodeId_WhenGetEpisodeByNameReturnsId_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        episode.IndexNumber = null;
        episode.Path = $"/mnt/abc/{new Faker().Random.Words()}.mp4";
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var episodeIdResult = new EpisodeIdResult(CrunchyrollIdFaker.Generate(), CrunchyrollSlugFaker.Generate());
        _mediator
            .Send(Arg.Any<EpisodeIdQueryByName>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<EpisodeIdResult?>(episodeIdResult));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episodeId.Should().Be(episodeIdResult.EpisodeId);
        
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var episodeSlugTitle).Should().BeTrue();
        episodeSlugTitle.Should().Be(episodeIdResult.EpisodeSlugTitle);
        
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<EpisodeIdQuery>(), Arg.Any<CancellationToken>());
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received().RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
    
    [Fact]
    public async Task SetsEmptyEpisodeIdAndRunsPostTasks_WhenCrunchyrollIdNotFound_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);
        
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episode.IndexNumber!.Value.ToString()))
            .Returns((EpisodeIdResult?)null);
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var slugTitle).Should().BeTrue();
        episodeId.Should().BeEmpty();
        slugTitle.Should().BeEmpty();
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received(1).RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
    
    [Fact]
    public async Task SkipsItem_WhenGetEpisodeFails_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);
        
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episode.IndexNumber!.Value.ToString()))
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeFalse();
        episodeId.Should().BeNull();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.DidNotReceive().RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
    
    [Fact]
    public async Task SkipsItem_WhenEpisodeHasAlreadyAnId_GivenSeasonWithSeasonIdAndEpisodeWithId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.GenerateWithEpisodeId(season);
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episode.IndexNumber!.Value.ToString()))
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episodeId.Should().NotBeNullOrEmpty();
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received(1).RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
    
    [Theory]
    [InlineData("E1124", "1124")]
    [InlineData("E502", "502")]
    [InlineData("E-FMI1", "FMI1")]
    [InlineData("E-FMI2", "FMI2")]
    [InlineData("S13E542", "542")]
    [InlineData("S13E-SP", "SP")]
    [InlineData("S13E-SP1 - abc", "SP1")]
    [InlineData("S1E6.5 - def", "6.5")]
    public async Task SetsEpisodeIdAndRunsPostTasks_WhenHasNoIndexNumberButWasFoundByName_GivenSeasonWithSeasonId(
        string episodeFileName, string episodeIdentifier)
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        var episode = EpisodeFaker.Generate();
        episode.Name = $"{episodeFileName} - {episode.Name}";
        episode.IndexNumber = null;
        episode.Path = $"/mnt/{new Faker().Random.Word()}/{episodeFileName} - {episode.Name}.mp4";
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episode]);
        
        MockHelper.MediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);

        var crunchyrollId = CrunchyrollIdFaker.Generate();
        var crunchyrollSlugTitle = CrunchyrollSlugFaker.Generate();
        _mediator
            .Send(new EpisodeIdQuery(
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                episodeIdentifier))
            .Returns(new EpisodeIdResult(crunchyrollId, crunchyrollSlugTitle));
        
        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, out var episodeId).Should().BeTrue();
        episode.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeSlugTitle, out var episodeSlugTitle).Should().BeTrue();
        episodeId.Should().Be(crunchyrollId);
        episodeSlugTitle.Should().Be(crunchyrollSlugTitle);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(
                Arg.Is<BaseItem>(x => x == episode), 
                Arg.Is<BaseItem>(x => x == episode.DisplayParent), 
                ItemUpdateType.MetadataEdit, 
                Arg.Any<CancellationToken>());

        _postSeasonIdSetTasks.Should().AllSatisfy(x =>
        {
            x.Received(1).RunAsync(episode, Arg.Any<CancellationToken>());
        });
    }
}