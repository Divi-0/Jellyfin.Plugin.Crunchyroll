using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan
{
    public class SetTitleIdTaskTests
    {
        private readonly SetTitleIdTask _sut;

        private readonly IMediator _mediator;
        private readonly IPostTitleIdSetTask[] _postTitleIdSetTasks;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        
        private readonly Faker _faker;

        public SetTitleIdTaskTests()
        {
            _mediator = Substitute.For<IMediator>();
            _postTitleIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
                .Select(_ => 
                    Substitute.For<IPostTitleIdSetTask>())
                .ToArray();
            var logger = Substitute.For<ILogger<SetTitleIdTask>>();
            _libraryManager = MockHelper.LibraryManager;
            _mediaSourceManager = MockHelper.MediaSourceManager;

            _sut = new SetTitleIdTask(_mediator, _postTitleIdSetTasks, logger, _libraryManager);

            _faker = new Faker();
        }

        [Fact]
        public async Task SetsTitleIdAndSlugTitle_WhenTitleIdWasReturned_GivenTitleWithNoCrunchyrollTitleId()
        {
            //Arrange
            var crunchrollId = CrunchyrollIdFaker.Generate();
            var crunchrollSlugTitle = CrunchyrollSlugFaker.Generate();
            var item = SeriesFaker.Generate();

            _mediator
                .Send(Arg.Is<TitleIdQuery>(x => x.Title == item.FileNameWithoutExtension), Arg.Any<CancellationToken>())
                .Returns(Result.Ok<SearchResponse?>(new SearchResponse
                {
                    Id = crunchrollId,
                    SlugTitle = crunchrollSlugTitle
                }));
            
            _libraryManager
                .UpdateItemAsync(Arg.Is<BaseItem>(x => x.Id == item.Id), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            _libraryManager
                .GetItemById(item.DisplayParentId)
                .Returns(SeriesFaker.Generate());

            _mediaSourceManager
                .GetPathProtocol(item.Path)
                .Returns(MediaProtocol.File);

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(Arg.Is<TitleIdQuery>(x => 
                    x.Title == item.FileNameWithoutExtension),
                    Arg.Any<CancellationToken>());
            
            await _libraryManager
                .Received(1)
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x.Id == item.Id), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());
            
            item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
            item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesSlugTitle, out var actualCrunchyrollSlugTitle).Should().BeTrue();
            actualCrunchyrollId.Should().Be(crunchrollId);
            actualCrunchyrollSlugTitle.Should().Be(crunchrollSlugTitle);
        }

        [Fact]
        public async Task CallsTitleIdQueryOnlyWithSeriesName_WhenFileNameHasYearAndDbId_GivenFileNameWithExtraLetters()
        {
            //Arrange
            var crunchrollId = CrunchyrollIdFaker.Generate();
            var crunchrollSlugTitle = CrunchyrollSlugFaker.Generate();
            var item = SeriesFaker.Generate();
            var seriesName = _faker.Random.Words(3);
            item.Path = $"{_faker.Random.Word()}/{seriesName} ({_faker.Random.Number()}) [tmdbid-{_faker.Random.Number()}]";

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Ok<SearchResponse?>(new SearchResponse
                {
                    Id = crunchrollId,
                    SlugTitle = crunchrollSlugTitle
                }));
            
            _libraryManager
                .UpdateItemAsync(Arg.Is<BaseItem>(x => x.Id == item.Id), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            _libraryManager
                .GetItemById(item.DisplayParentId)
                .Returns(SeriesFaker.Generate());

            _mediaSourceManager
                .GetPathProtocol(item.Path)
                .Returns(MediaProtocol.File);

            //Act
            await _sut.RunAsync(item, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(Arg.Is<TitleIdQuery>(x => 
                    x.Title == seriesName),
                    Arg.Any<CancellationToken>());
            
            await _libraryManager
                .Received(1)
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x.Id == item.Id), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());
            
            item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
            item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesSlugTitle, out var actualCrunchyrollSlugTitle).Should().BeTrue();
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
                .UpdateItemAsync(Arg.Is<BaseItem>(x => x.Id == item.Id), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            _libraryManager
                .GetItemById(item.DisplayParentId)
                .Returns(SeriesFaker.Generate());
            
            _mediaSourceManager
                .GetPathProtocol(item.Path)
                .Returns(MediaProtocol.File);

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
            item.ProviderIds.Add(CrunchyrollExternalKeys.SeriesId, CrunchyrollIdFaker.Generate());
            
            _mediaSourceManager
                .GetPathProtocol(item.Path)
                .Returns(MediaProtocol.File);

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
            
            _mediaSourceManager
                .GetPathProtocol(item.Path)
                .Returns(MediaProtocol.File);

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
                    x.Title == item.FileNameWithoutExtension),
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
            
            _mediaSourceManager
                .GetPathProtocol(item.Path)
                .Returns(MediaProtocol.File);

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
                    x.Title == item.FileNameWithoutExtension),
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
            
            _mediaSourceManager
                .GetPathProtocol(series.Path)
                .Returns(MediaProtocol.File);

            _mediator
                .Send(Arg.Any<TitleIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result.Ok<SearchResponse?>(null));
            
            _libraryManager
                .UpdateItemAsync(Arg.Is<BaseItem>(x => x.Id == series.Id), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
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
                    x.Title == series.FileNameWithoutExtension),
                    Arg.Any<CancellationToken>());
            
            await _libraryManager
                .Received(1)
                .UpdateItemAsync(
                    Arg.Is<BaseItem>(x => x.Id == series.Id), 
                    Arg.Any<BaseItem>(), 
                    ItemUpdateType.MetadataEdit, 
                    Arg.Any<CancellationToken>());
            
            series.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
            series.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesSlugTitle, out var actualCrunchyrollSlugTitle).Should().BeTrue();
            actualCrunchyrollId.Should().Be(string.Empty);
            actualCrunchyrollSlugTitle.Should().Be(string.Empty);
        }
    }
}
