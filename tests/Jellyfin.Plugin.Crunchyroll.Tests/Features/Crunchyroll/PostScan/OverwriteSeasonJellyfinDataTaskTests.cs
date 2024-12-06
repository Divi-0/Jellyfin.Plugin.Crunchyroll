using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteSeasonJellyfinDataTaskTests
{
    private readonly OverwriteSeasonJellyfinDataTask _sut;
    private readonly IOverwriteSeasonJellyfinDataRepository _repository;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemRepository _itemRepository;
    private readonly PluginConfiguration _config;

    public OverwriteSeasonJellyfinDataTaskTests()
    {
        var logger = Substitute.For<ILogger<OverwriteSeasonJellyfinDataTask>>();
        _libraryManager = MockHelper.LibraryManager;
        _itemRepository = MockHelper.ItemRepository;
        _repository = Substitute.For<IOverwriteSeasonJellyfinDataRepository>();
        _config = new PluginConfiguration
        {
            IsFeatureSeasonTitleEnabled = true,
            IsFeatureSeasonOrderByCrunchyrollOrderEnabled = true
        };

        _sut = new OverwriteSeasonJellyfinDataTask(logger, _repository, _libraryManager, _config);
    }

    [Fact]
    public async Task SetsMetadata_WhenSuccessful_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([EpisodeFaker.Generate(season)]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        season.Name.Should().Be($"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}");

        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .Received(1)
            .UpdateItemAsync(season, null!, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NameDoesNotContainSeasonNumber_WhenSeasonDisplayNumberIsEmpty_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);
        crunchyrollSeason = crunchyrollSeason with { SeasonDisplayNumber = string.Empty };

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([EpisodeFaker.Generate(season)]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        season.Name.Should().Be(crunchyrollSeason.Title);

        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .Received(1)
            .UpdateItemAsync(season, null!, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipsItem_WhenItemHasNoSeasonId_GivenSeasonWithoutSeasonId()
    {
        //Arrange
        var season = SeasonFaker.Generate();

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        await _repository
            .DidNotReceive()
            .GetSeasonAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotUpdateMetadata_WhenGetCrunchyrollSeasonFails_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotThrow_WhenLibraryManagerThrows_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([EpisodeFaker.Generate(season)]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);
        
        _libraryManager
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception());

        //Act & Assert
        var action = async () => await _sut.RunAsync(season, CancellationToken.None);
        await action.Should().NotThrowAsync();

        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .Received()
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrdersSeasonsBySeasonSequenceNumber_WhenFeatureOrderByCrunchyrollOrderIsEnabled_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);
        var episodeChild = EpisodeFaker.Generate(season);

        var oldPresentationKey = season.PresentationUniqueKey;
        
        _config.IsFeatureSeasonOrderByCrunchyrollOrderEnabled = true;

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episodeChild]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        season.Name.Should().Be($"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}");
        season.IndexNumber.Should().Be(crunchyrollSeason.SeasonSequenceNumber);
        season.PresentationUniqueKey.Should().NotBe(oldPresentationKey);

        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .Received(1)
            .UpdateItemAsync(season, null!, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        episodeChild.ParentIndexNumber.Should().Be(crunchyrollSeason.SeasonSequenceNumber);
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episodeChild, season, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotOrdersSeasonsBySeasonSequenceNumber_WhenFeatureOrderByCrunchyrollOrderIsDisabled_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);
        var episodeChild = EpisodeFaker.Generate(season);
        
        _config.IsFeatureSeasonOrderByCrunchyrollOrderEnabled = false;

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([episodeChild]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        season.Name.Should().Be($"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}");
        season.IndexNumber.Should().NotBe(crunchyrollSeason.SeasonSequenceNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .Received(1)
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
        
        episodeChild.ParentIndexNumber.Should().NotBe(crunchyrollSeason.SeasonSequenceNumber);
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episodeChild, season, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsItem_WhenRepositoryGetSeasonReturnedNull_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([EpisodeFaker.Generate(season)]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId], 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Season?>(null));

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(season, null!, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotSetTitle_WhenFeatureSeasonTitleIsDisabled_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);

        _config.IsFeatureSeasonTitleEnabled = false;

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(x =>
                x.ParentId == season.Id &&
                x.GroupByPresentationUniqueKey == false &&
                x.DtoOptions.Fields.Count != 0))
            .Returns([EpisodeFaker.Generate(season)]);

        _repository
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        season.Name.Should().NotContain(crunchyrollSeason.Title);

        await _repository
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId],
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _libraryManager
            .Received(1)
            .UpdateItemAsync(season, null!, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
}