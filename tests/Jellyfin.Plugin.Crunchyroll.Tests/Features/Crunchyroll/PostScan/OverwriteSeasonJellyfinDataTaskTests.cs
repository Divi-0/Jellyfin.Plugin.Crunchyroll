using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;
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