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
}