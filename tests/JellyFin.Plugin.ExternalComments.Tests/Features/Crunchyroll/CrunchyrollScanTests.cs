using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Mediator;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll;

public class CrunchyrollScanTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollScan _sut;
    private readonly ILogger<CrunchyrollScan> _loggerMock;
    private readonly ILibraryManager _libraryManagerMock;
    private readonly IItemRepository _itemRepository;
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;
    private readonly ILoginService _loginService;

    public CrunchyrollScanTests()
    {
        _fixture = new Fixture();
        
        _loggerMock = Substitute.For<ILogger<CrunchyrollScan>>();
        _libraryManagerMock = Substitute.For<ILibraryManager>();
        _itemRepository = Substitute.For<IItemRepository>();
        _mediator = Substitute.For<IMediator>();
        _loginService = Substitute.For<ILoginService>();
        _config = new PluginConfiguration();
        _sut = new CrunchyrollScan(_loggerMock, _libraryManagerMock, _config, _mediator, _loginService);
    }
    
    [Fact]
    public async Task SetsCrunchyrollIdsAndExtractReviews_WhenTitleIdIsFound_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        var itemList = _fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _libraryManagerMock
            .GetItemById(Arg.Any<Guid>())
            .Returns(_fixture.Create<Series>());
        
        _libraryManagerMock
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(_fixture.CreateMany<Season>().ToList<BaseItem>());

        var titleIds = new List<string>();
        var slugTitles = new List<string>();
        var seasonIds = new List<string>();
        
        foreach (var item in itemList.Select(x => (Series)x).ToList())
        {
            var titleId = _fixture.Create<string>();
            var slugTitle = _fixture.Create<string>();
            _mediator
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));
            
            titleIds.Add(titleId);
            slugTitles.Add(slugTitle);

            //Find Season By Number
            foreach (var season in item.Children.Where(x => x.IndexNumber.HasValue))
            {
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, 0))
                    .Returns(seasonId);
                
                seasonIds.Add(seasonId);
            }
        }

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        foreach (var item in itemList)
        {
            item.ProviderIds[CrunchyrollExternalKeys.Id].Should().BeOneOf(titleIds);
            item.ProviderIds[CrunchyrollExternalKeys.SlugTitle].Should().BeOneOf(slugTitles);
            
            await _mediator
                .Received(1)
                .Send(new ExtractReviewsCommand()
                {
                    TitleId = item.ProviderIds[CrunchyrollExternalKeys.Id], 
                    SlugTitle = item.ProviderIds[CrunchyrollExternalKeys.SlugTitle]
                }, Arg.Any<CancellationToken>());
            
            await _mediator
                .Received(1)
                .Send(new ScrapTitleMetadataCommand()
                {
                    TitleId = item.ProviderIds[CrunchyrollExternalKeys.Id], 
                    SlugTitle = item.ProviderIds[CrunchyrollExternalKeys.SlugTitle]
                }, Arg.Any<CancellationToken>());
            
            //Season Ids were set
            foreach (var season in ((Series)item).Children)
            {
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().BeOneOf(seasonIds);
            }
        }
    }
    
    [Fact]
    public async Task SetsSeasonIds_WhenSeasonIdByNumberNotFoundAndSearchesByNameInstead_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        var itemList = _fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _libraryManagerMock
            .GetItemById(Arg.Any<Guid>())
            .Returns(_fixture.Create<Series>());
        
        _libraryManagerMock
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(_fixture.CreateMany<Season>().ToList<BaseItem>());
        
        var seasonIds = new List<string>();
        
        foreach (var item in itemList.Select(x => (Series)x).ToList())
        {
            var titleId = _fixture.Create<string>();
            var slugTitle = _fixture.Create<string>();
            _mediator
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));

            //Find Season By Number
            foreach (var season in item.Children.Skip(1).Where(x => x.IndexNumber.HasValue))
            {
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, 0))
                    .Returns(seasonId);
                
                seasonIds.Add(seasonId);
            }
            
            _mediator
                .Send(new SeasonIdQueryByNumber(titleId, item.Children.First().IndexNumber!.Value, 0))
                .Returns((string?)null);
            
            var byNameSeasonId = _fixture.Create<string>();
            _mediator
                .Send(new SeasonIdQueryByName(titleId, item.Children.First().Name))
                .Returns(byNameSeasonId);
                
            seasonIds.Add(byNameSeasonId);
        }

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        foreach (var item in itemList)
        {
            //Season Ids were set
            foreach (var season in ((Series)item).Children)
            {
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().BeOneOf(seasonIds);
            }
        }
    }
    
    [Fact]
    public async Task IgnoresChild_WhenChildrenIndexNumberIsNull_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        var itemList = _fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _libraryManagerMock
            .GetItemById(Arg.Any<Guid>())
            .Returns(_fixture.Create<Series>());
        
        _libraryManagerMock
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var children = 
            _fixture.CreateMany<Season>()
            .ToList<BaseItem>();
        
        var childWithNoIndexNumber = _fixture.Build<Season>()
            .With(x => x.IndexNumber, (int?)null)
            .Create();
        
        children.Add(childWithNoIndexNumber);
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(children);
        
        foreach (var item in itemList.Select(x => (Series)x).ToList())
        {
            var titleId = _fixture.Create<string>();
            var slugTitle = _fixture.Create<string>();
            _mediator
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));

            //Find Season By Number
            foreach (var season in item.Children.Where(x => x.IndexNumber.HasValue))
            {
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, 1))
                    .Returns(seasonId);
            }
        }

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        childWithNoIndexNumber.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out _).Should().BeFalse();
    }
    
    [Fact]
    public async Task IgnoresChild_WhenChildrenHasAlreadySeasonIdSet_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        var itemList = _fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _libraryManagerMock
            .GetItemById(Arg.Any<Guid>())
            .Returns(_fixture.Create<Series>());
        
        _libraryManagerMock
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var children = 
            _fixture.CreateMany<Season>()
            .ToList<BaseItem>();
        
        var childSeasonId = Guid.NewGuid().ToString();
        var childWithAlreadySetSeasonId = _fixture.Create<Season>();
        childWithAlreadySetSeasonId.ProviderIds[CrunchyrollExternalKeys.SeasonId] = childSeasonId;
        
        
        children.Add(childWithAlreadySetSeasonId);
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(children);
        
        foreach (var item in itemList.Select(x => (Series)x).ToList())
        {
            var titleId = _fixture.Create<string>();
            var slugTitle = _fixture.Create<string>();
            _mediator
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));

            //Find Season By Number
            foreach (var season in item.Children.Where(x => x.IndexNumber.HasValue))
            {
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, 1))
                    .Returns(seasonId);
            }
        }

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        childWithAlreadySetSeasonId.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(childSeasonId);
    }
    
    [Fact]
    public async Task CountsUpSeasonDuplicateNumber_WhenMultipleSeasonsWithTheSameIndexSeasonNumber_GivenListOfDuplicateSeasons()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        var itemList = _fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _libraryManagerMock
            .GetItemById(Arg.Any<Guid>())
            .Returns(_fixture.Create<Series>());
        
        _libraryManagerMock
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        const int indexNumber = 123;
        var children = 
            _fixture.Build<Season>()
                .With(x => x.IndexNumber, indexNumber)
                .CreateMany()
                .ToList<BaseItem>();
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(children);
        
        foreach (var item in itemList.Select(x => (Series)x).ToList())
        {
            var titleId = _fixture.Create<string>();
            var slugTitle = _fixture.Create<string>();
            _mediator
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));

            //Find Season By Number
            for (var i = 0; i < item.Children.Where(x => x.IndexNumber.HasValue).ToList().Count; i++)
            {
                var season = item.Children.Where(x => x.IndexNumber.HasValue).ToList()[i];
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, i))
                    .Returns(seasonId);
            }
        }

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        for (var i = 0; i < children.Count; i++)
        {
            var currentDuplicateCounter = i;
            await _mediator
                .Received()
                .Send(Arg.Is<SeasonIdQueryByNumber>(x => 
                    x.SeasonNumber == indexNumber &&
                    x.DuplicateCounter == currentDuplicateCounter),
                    Arg.Any<CancellationToken>());
        }
    }
    
    [Fact]
    public async Task SkipsItem_WhenItemHasExistingCrunchyrollId_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        var itemList = _fixture.Build<Series>()
            .With(x => x.ProviderIds, 
                new Dictionary<string, string>(){{CrunchyrollExternalKeys.Id, _fixture.Create<string>()}})
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(_fixture.CreateMany<Season>().ToList<BaseItem>());
        
        foreach (var item in itemList.Select(x => (Series)x).ToList())
        {
            //Find Season By Number
            foreach (var season in item.Children.Where(x => x.IndexNumber.HasValue))
            {
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(item.ProviderIds[CrunchyrollExternalKeys.Id], season.IndexNumber!.Value, 1))
                    .Returns(seasonId);
            }
        }
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _mediator
            .Received(0)
            .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsExtractReviews_WhenIsWaybackMachineIsDisabled_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        BaseItem.ItemRepository = _itemRepository;
        
        _config.IsWaybackMachineEnabled = false;
        
        var itemList = _fixture.Build<Series>()
            .CreateMany<Series>()
            .ToList<BaseItem>();
        
        _libraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(itemList);
        
        _libraryManagerMock
            .GetItemById(Arg.Any<Guid>())
            .Returns(_fixture.Create<Series>());
        
        _libraryManagerMock
            .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        _itemRepository
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(_fixture.CreateMany<Season>().ToList<BaseItem>());
        
        foreach (var item in itemList)
        {
            var titleId = _fixture.Create<string>();
            var slugTitle = _fixture.Create<string>();
            _mediator
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));
            
            foreach (var season in ((Series)item).Children.Where(x => x.IndexNumber.HasValue))
            {
                var seasonId = _fixture.Create<string>();
                _mediator
                    .Send(new SeasonIdQueryByNumber(titleId, season.IndexNumber!.Value, 1))
                    .Returns(seasonId);
            }
        }
        
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }
}