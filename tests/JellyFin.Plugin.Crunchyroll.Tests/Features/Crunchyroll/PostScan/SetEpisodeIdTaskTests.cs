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
using Mediator;
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
            
        _sut = new SetEpisodeIdTask(_mediator, _libraryManager, logger, _postSeasonIdSetTasks);
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
        
        _mediator
            .Send(new EpisodeIdQuery(
                series.ProviderIds[CrunchyrollExternalKeys.Id], 
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

    [Fact]
    public async Task IgnoresItem_WhenNoTitleIdFound_GivenSeasonWithSeriesWithoutTitleId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
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
    public async Task SkipItem_WhenEpisodeHasNoIndexNumber_GivenSeasonWithSeasonId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        
        _libraryManager
            .GetItemById(series.Id)
            .Returns(series);
        
        var episode = EpisodeFaker.Generate();
        episode.IndexNumber = null;
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
        
        _mediator
            .Send(new EpisodeIdQuery(
                series.ProviderIds[CrunchyrollExternalKeys.Id], 
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
        
        _mediator
            .Send(new EpisodeIdQuery(
                series.ProviderIds[CrunchyrollExternalKeys.Id], 
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
    public async Task SkipsItem_WhenEpisodeHasAlreadyAId_GivenSeasonWithSeasonIdAndEpisodeWithId()
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
                series.ProviderIds[CrunchyrollExternalKeys.Id], 
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
}