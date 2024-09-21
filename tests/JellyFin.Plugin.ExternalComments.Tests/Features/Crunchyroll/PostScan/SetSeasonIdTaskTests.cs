using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Mediator;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using MediaBrowser.Controller.Persistence;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using FluentAssertions;
using FluentResults;
using NSubstitute.ExceptionExtensions;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.PostScan
{
    public class SetSeasonIdTaskTests
    {
        private readonly Fixture _fixture;

        private readonly SetSeasonIdTask _sut;

        private readonly IMediator _mediator;
        private readonly IPostSeasonIdSetTask[] _postSeasonIdSetTasks;
        private readonly ILibraryManager _libraryManager;
        private readonly IItemRepository _itemRepository;

        public SetSeasonIdTaskTests()
        {
            _fixture = new Fixture();
            
            _postSeasonIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => Substitute.For<IPostSeasonIdSetTask>())
            .ToArray();
            
            _mediator = Substitute.For<IMediator>();
            _libraryManager = MockHelper.LibraryManager;
            _itemRepository = MockHelper.ItemRepository;
            var logger = Substitute.For<ILogger<SetSeasonIdTask>>();
            
            _sut = new SetSeasonIdTask(_mediator, _postSeasonIdSetTasks, logger, _libraryManager);
        }

        [Fact]
        public async Task SetsSeasonIdAndRunsPostTasks_WhenIdFoundWithName_GivenItemWithUniqueName()
        {
            //Arrange
            var series = SeriesFaker.GenerateWithTitleId();

            var season = SeasonFaker.Generate(series);
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns([season]);

            var crunchyrollSeasonId = CrunchyrollIdFaker.Generate();
            _mediator
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], season.Name), 
                    Arg.Any<CancellationToken>())
                .Returns(crunchyrollSeasonId);

            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            var child = series.Children.First();

            await _mediator
                .Received(1)
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], child.Name),
                    Arg.Any<CancellationToken>());

            await _libraryManager
                .Received(1)
                .UpdateItemAsync(child, series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());

            child.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeasonId, out var seasonId).Should().BeTrue();
            seasonId.Should().Be(crunchyrollSeasonId);

            foreach (var seasonItem in series.Children)
            {
                _postSeasonIdSetTasks.Should().AllSatisfy(x =>
                {
                    x.Received(1).RunAsync(seasonItem, Arg.Any<CancellationToken>());
                });
            }
        }

        [Fact]
        public async Task RunsPostTasks_WhenSetSeasonIdFails_GivenItemHasUniqueName()
        {
            //Arrange
            var series = SeriesFaker.GenerateWithTitleId();

            var season = SeasonFaker.Generate(series);
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns([season]);
            
            _mediator
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], season.Name), 
                    Arg.Any<CancellationToken>())
                .Returns(Result.Fail("error"));

            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            var child = series.Children.First();

            await _mediator
                .Received(1)
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], child.Name),
                    Arg.Any<CancellationToken>());

            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(child, series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());

            foreach (var seasonItem in series.Children)
            {
                _postSeasonIdSetTasks.Should().AllSatisfy(x =>
                {
                    x.Received(1).RunAsync(seasonItem, Arg.Any<CancellationToken>());
                });
            }
        }

        [Fact]
        public async Task SkipsAllItems_WhenSeriesHasNoTitleId_GivenItemWithNoTitleId()
        {
            //Arrange
            var series = SeriesFaker.Generate();
            
            var seasons = Enumerable.Range(0, Random.Shared.Next(1, 10))
                .Select(_ => SeasonFaker.Generate(series))
                .ToList<BaseItem>();
            
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns(seasons);
            
            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            foreach (var seasonItem in seasons)
            {
                await _mediator
                    .DidNotReceive()
                    .Send(Arg.Is<SeasonIdQueryByName>(x => x.SeasonName == seasonItem.Name),
                        Arg.Any<CancellationToken>());
                
                await _libraryManager
                    .DidNotReceive()
                    .UpdateItemAsync(seasonItem, series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
                
                _postSeasonIdSetTasks.Should().AllSatisfy(x =>
                {
                    x.Received(1).RunAsync(seasonItem, Arg.Any<CancellationToken>());
                });
            }
        }

        [Fact]
        public async Task SkipsAllItems_WhenSeasonsAlreadyHaveASeasonId_GivenItemWithSeasonsThatHaveSeasonIds()
        {
            //Arrange
            var series = SeriesFaker.GenerateWithTitleId();
            
            var seasons = Enumerable.Range(0, Random.Shared.Next(1, 10))
                .Select(_ => SeasonFaker.GenerateWithSeasonId(series))
                .ToList<BaseItem>();
            
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns(seasons);
            
            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            foreach (var seasonItem in seasons)
            {
                await _mediator
                    .DidNotReceive()
                    .Send(Arg.Is<SeasonIdQueryByName>(x => x.SeasonName == seasonItem.Name),
                        Arg.Any<CancellationToken>());
                
                await _libraryManager
                    .DidNotReceive()
                    .UpdateItemAsync(seasonItem, series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
                
                _postSeasonIdSetTasks.Should().AllSatisfy(x =>
                {
                    x.Received(1).RunAsync(seasonItem, Arg.Any<CancellationToken>());
                });
            }
        }

        [Fact]
        public async Task SkipsItemButRunsPostTasks_WhenGetSeasonIdByNameFails_GivenItemWithUniqueName()
        {
            //Arrange
            var series = SeriesFaker.GenerateWithTitleId();

            var seasons = Enumerable.Range(0, Random.Shared.Next(1, 10))
                .Select(_ => SeasonFaker.Generate(series))
                .ToList<BaseItem>();

            _mediator
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[0].Name), 
                    Arg.Any<CancellationToken>())
                .Returns(Result.Fail("error"));

            for (int i = 1; i < seasons.Count - 1; i++)
            {
                _mediator
                    .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[i].Name), 
                        Arg.Any<CancellationToken>())
                    .Returns(CrunchyrollIdFaker.Generate());
            }
            
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns(seasons);
            
            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[0].Name),
                    Arg.Any<CancellationToken>());
                
            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(seasons[0], series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
            
            _postSeasonIdSetTasks.Should().AllSatisfy(x =>
            {
                x.Received(1).RunAsync(seasons[0], Arg.Any<CancellationToken>());
            });
            
            for (int i = 1; i < seasons.Count - 1; i++)
            {
                await _mediator
                    .Received(1)
                    .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[i].Name),
                        Arg.Any<CancellationToken>());
                
                await _libraryManager
                    .Received(1)
                    .UpdateItemAsync(seasons[i], series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
                
                _postSeasonIdSetTasks.Should().AllSatisfy(x =>
                {
                    x.Received(1).RunAsync(seasons[i], Arg.Any<CancellationToken>());
                });
            }
        }

        [Fact]
        public async Task SkipsItemButRunsPostTasks_WhenGetSeasonIdByNumberFails_GivenItemWithUniqueName()
        {
            //Arrange
            var series = SeriesFaker.GenerateWithTitleId();

            var seasons = Enumerable.Range(0, Random.Shared.Next(1, 10))
                .Select(_ => SeasonFaker.Generate(series))
                .ToList<BaseItem>();

            _mediator
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[0].Name), 
                    Arg.Any<CancellationToken>())
                .Returns(Result.Ok<string?>(null));            
            
            _mediator
                .Send(new SeasonIdQueryByNumber(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[0].IndexNumber!.Value, 0), 
                    Arg.Any<CancellationToken>())
                .Returns(Result.Fail("error"));

            for (int i = 1; i < seasons.Count - 1; i++)
            {
                _mediator
                    .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[i].Name), 
                        Arg.Any<CancellationToken>())
                    .Returns(CrunchyrollIdFaker.Generate());
            }
            
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns(seasons);
            
            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(Arg.Is<SeasonIdQueryByName>(x => x.SeasonName == seasons[0].Name),
                    Arg.Any<CancellationToken>());
            
            await _mediator
                .Received(1)
                .Send(new SeasonIdQueryByNumber(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[0].IndexNumber!.Value, 0),
                    Arg.Any<CancellationToken>());
                
            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(seasons[0], series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
            
            _postSeasonIdSetTasks.Should().AllSatisfy(x =>
            {
                x.Received(1).RunAsync(seasons[0], Arg.Any<CancellationToken>());
            });
            
            for (int i = 1; i < seasons.Count - 1; i++)
            {
                await _mediator
                    .Received(1)
                    .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], seasons[i].Name),
                        Arg.Any<CancellationToken>());
                
                await _libraryManager
                    .Received(1)
                    .UpdateItemAsync(seasons[i], series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
                
                _postSeasonIdSetTasks.Should().AllSatisfy(x =>
                {
                    x.Received(1).RunAsync(seasons[i], Arg.Any<CancellationToken>());
                });
            }
        }
        
        

        [Fact]
        public async Task SkipsItemButRunsPostTasks_WhenExceptionIsThrownWhileSettingSeasonId_GivenItemWithUniqueName()
        {
            //Arrange
            var series = SeriesFaker.GenerateWithTitleId();

            var season = SeasonFaker.Generate(series);

            _mediator
                .Send(new SeasonIdQueryByName(series.ProviderIds[CrunchyrollExternalKeys.Id], season.Name),
                    Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception());
            
            _itemRepository
                .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                    x.ParentId == series.Id &&
                    x.GroupByPresentationUniqueKey == false &&
                    x.DtoOptions.Fields.Count != 0))
                .Returns([season]);
            
            //Act
            await _sut.RunAsync(series, CancellationToken.None);

            //Assert
            await _mediator
                .Received(1)
                .Send(Arg.Is<SeasonIdQueryByName>(x => x.SeasonName == season.Name),
                    Arg.Any<CancellationToken>());
                
            await _libraryManager
                .DidNotReceive()
                .UpdateItemAsync(season, series, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
            
            _postSeasonIdSetTasks.Should().AllSatisfy(x =>
            {
                x.Received(1).RunAsync(season, Arg.Any<CancellationToken>());
            });
        }
    }
}
