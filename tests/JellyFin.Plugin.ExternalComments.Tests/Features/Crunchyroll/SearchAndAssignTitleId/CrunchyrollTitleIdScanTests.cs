using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.SearchAndAssignTitleId;

public class CrunchyrollTitleIdScanTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollTitleIdScan _sut;
    private readonly ILogger<CrunchyrollTitleIdScan> _loggerMock;
    private readonly ILibraryManager _libraryManagerMock;
    private readonly IMediator _mediatorMock;

    public CrunchyrollTitleIdScanTests()
    {
        _fixture = new Fixture();
        
        _loggerMock = Substitute.For<ILogger<CrunchyrollTitleIdScan>>();
        _libraryManagerMock = Substitute.For<ILibraryManager>();
        _mediatorMock = Substitute.For<IMediator>();
        _sut = new CrunchyrollTitleIdScan(_libraryManagerMock, _mediatorMock);
    }
    
    [Fact]
    public async Task SetsCrunchyrollIds_WhenTitleIdIsFound_GivenListOfItems()
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
            _mediatorMock
                .Send(new TitleIdQuery(item.Name), Arg.Any<CancellationToken>())!
                .Returns(ValueTask.FromResult(Result.Ok(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slugTitle
                })));
            
            titleIds.Add(titleId);
            slugTitles.Add(slugTitle);
        }
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        foreach (var item in itemList)
        {
            item.ProviderIds[CrunchyrollExternalKeys.Id].Should().BeOneOf(titleIds);
            item.ProviderIds[CrunchyrollExternalKeys.SlugTitle].Should().BeOneOf(slugTitles);
            
            await _mediatorMock
                .Received(1)
                .Send(new ExtractReviewsCommand()
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
        
        //Act
        var progress = new Progress<double>();
        await _sut.Run(progress, CancellationToken.None);
        
        //Assert
        await _mediatorMock
            .Received(0)
            .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>());
    }
}