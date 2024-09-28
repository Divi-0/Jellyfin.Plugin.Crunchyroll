using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.PostScan
{
    public class SetTitleIdTaskTests
    {
        private readonly SetTitleIdTask _sut;

        private readonly IMediator _mediator;
        private readonly IPostTitleIdSetTask[] _postTitleIdSetTasks;
        private readonly ILibraryManager _libraryManager;

        public SetTitleIdTaskTests()
        {
            _mediator = Substitute.For<IMediator>();
            _postTitleIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
                .Select(_ => 
                    Substitute.For<IPostTitleIdSetTask>())
                .ToArray();
            var logger = Substitute.For<ILogger<SetTitleIdTask>>();
            _libraryManager = MockHelper.LibraryManager;

            _sut = new SetTitleIdTask(_mediator, _postTitleIdSetTasks, logger, _libraryManager);
        }

        [Fact]
        public async Task SetsTitleIdAndSlugTitle_WhenTitleIdWasReturned_GivenTitleWithNoCrunchyrollTitleId()
        {
            //Arrange
            var crunchrollId = CrunchyrollIdFaker.Generate();
            var crunchrollSlugTitle = CrunchyrollSlugFaker.Generate();
            var item = SeriesFaker.Generate();

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Ok<SearchResponse?>(new SearchResponse
                {
                    Id = crunchrollId,
                    SlugTitle = crunchrollSlugTitle
                }));

            BaseItem updatedBaseItem = null!;
            _libraryManager
                .UpdateItemAsync(Arg.Do<BaseItem>(x => updatedBaseItem = x), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            _libraryManager
                .GetItemById(item.DisplayParentId)
                .Returns(SeriesFaker.Generate());

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(Arg.Is<TitleIdQuery>(x => 
                    x.Title == item.Name),
                    Arg.Any<CancellationToken>());
            
            await _libraryManager
                .Received(1)
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x == item), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());

            updatedBaseItem.Should().NotBeNull();
            updatedBaseItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out var actualCrunchyrollId).Should().BeTrue();
            updatedBaseItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SlugTitle, out var actualCrunchyrollSlugTitle).Should().BeTrue();
            actualCrunchyrollId.Should().Be(crunchrollId);
            actualCrunchyrollSlugTitle.Should().Be(crunchrollSlugTitle);
        }

        [Fact]
        public async Task CallsAllPostTitleIdSetTasks_WhenTitleIdWasSet_GivenTitleWithNoCrunchyrollTitleId()
        {
            //Arrange
            var crunchrollId = CrunchyrollIdFaker.Generate();
            var crunchrollSlugTitle = CrunchyrollSlugFaker.Generate();
            var item = SeriesFaker.Generate();

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Ok<SearchResponse?>(new SearchResponse
                {
                    Id = crunchrollId,
                    SlugTitle = crunchrollSlugTitle
                }));

            _libraryManager
                .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            _libraryManager
                .GetItemById(item.DisplayParentId)
                .Returns(SeriesFaker.Generate());

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            _postTitleIdSetTasks.Should().AllSatisfy(task =>
            {
                task
                .Received(1)
                .RunAsync(item, Arg.Any<CancellationToken>());
            });
        }

        [Fact]
        public async Task OnlyCallsPostTasks_WhenItemHasAlreadyACrunchyrollId_GivenTitleWithCrunchyrollTitleId()
        {
            //Arrange
            var item = SeriesFaker.Generate();
            item.ProviderIds.Add(CrunchyrollExternalKeys.Id, CrunchyrollIdFaker.Generate());

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            _postTitleIdSetTasks.Should().AllSatisfy(task =>
            {
                task
                .Received(1)
                .RunAsync(item, Arg.Any<CancellationToken>());
            });

            await _mediator
                .DidNotReceive()
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>());
            
            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x == item), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DoesNotUpdateItem_WhenGetTitleIdFails_GivenTitleWithNoCrunchyrollTitleId()
        {
            //Arrange
            var item = SeriesFaker.Generate();

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Fail("error"));

            _libraryManager
                .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            _postTitleIdSetTasks.Should().AllSatisfy(task =>
            {
                task
                .Received(1)
                .RunAsync(item, Arg.Any<CancellationToken>());
            });

            await _mediator
                .Received(1)
                .Send(Arg.Is<TitleIdQuery>(x =>
                    x.Title == item.Name),
                    Arg.Any<CancellationToken>());

            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(
                    item, 
                    Arg.Any<BaseItem>(), 
                    Arg.Any<ItemUpdateType>(), 
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DoesNotUpdateItem_WhenGetTitleIdThrows_GivenTitleWithNoCrunchyrollTitleId()
        {
            //Arrange
            var item = SeriesFaker.Generate();

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception());

            _libraryManager
                .UpdateItemAsync(Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            _postTitleIdSetTasks.Should().AllSatisfy(task =>
            {
                task
                .DidNotReceive()
                .RunAsync(item, Arg.Any<CancellationToken>());
            });

            await _mediator
                .Received(1)
                .Send(Arg.Is<TitleIdQuery>(x =>
                    x.Title == item.Name),
                    Arg.Any<CancellationToken>());
            
            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x == item), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetsEmptyTitleIdAndSlugTitle_WhenNoTitleIdWasFound_GivenTitleWithNoCrunchyrollTitleId()
        {
            //Arrange
            var series = SeriesFaker.Generate();

            MockHelper.LibraryManager
                .GetItemById(series.Id)
                .Returns(series);

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Ok<SearchResponse?>(null));

            BaseItem updatedBaseItem = null!;
            _libraryManager
                .UpdateItemAsync(Arg.Do<BaseItem>(x => updatedBaseItem = x), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            _libraryManager
                .GetItemById(series.DisplayParentId)
                .Returns(SeriesFaker.Generate());

            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(Arg.Is<TitleIdQuery>(x =>
                    x.Title == series.Name),
                    Arg.Any<CancellationToken>());
            
            await _libraryManager
                .Received(1)
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x == series), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());

            updatedBaseItem.Should().NotBeNull();
            updatedBaseItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out var actualCrunchyrollId).Should().BeTrue();
            updatedBaseItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SlugTitle, out var actualCrunchyrollSlugTitle).Should().BeTrue();
            actualCrunchyrollId.Should().Be(string.Empty);
            actualCrunchyrollSlugTitle.Should().Be(string.Empty);
        }
    }
}
