using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll;

public class CrunchyrollScanTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollScan _sut;
    private readonly ILogger<CrunchyrollScan> _loggerMock;
    private readonly ILibraryManager _libraryManagerMock;
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;
    private readonly ILoginService _loginService;

    public CrunchyrollScanTests()
    {
        _fixture = new Fixture();
        
        _loggerMock = Substitute.For<ILogger<CrunchyrollScan>>();
        _libraryManagerMock = Substitute.For<ILibraryManager>();
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

        var titleIds = new List<string>();
        var slugTitles = new List<string>();
        
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
            
            titleIds.Add(titleId);
            slugTitles.Add(slugTitle);
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
        }
    }
    
    [Fact]
    public async Task SkipsItem_WhenItemHasExistingCrunchyrollId_GivenListOfItems()
    {
        //Arrange
        BaseItem.LibraryManager = _libraryManagerMock;
        
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