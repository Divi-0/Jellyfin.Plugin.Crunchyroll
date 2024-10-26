using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteSeasonJellyfinDataTaskTests
{
    private readonly OverwriteSeasonJellyfinDataTask _sut;
    private readonly IOverwriteSeasonJellyfinDataSession _session;
    private readonly ILibraryManager _libraryManager;

    public OverwriteSeasonJellyfinDataTaskTests()
    {
        var logger = Substitute.For<ILogger<OverwriteSeasonJellyfinDataTask>>();
        _libraryManager = MockHelper.LibraryManager;
        _session = Substitute.For<IOverwriteSeasonJellyfinDataSession>();

        _sut = new OverwriteSeasonJellyfinDataTask(logger, _session, _libraryManager);
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

        _session
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId])
            .Returns(crunchyrollSeason);

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        season.Name.Should().Be(crunchyrollSeason.Title);

        await _session
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId]);

        await _libraryManager
            .Received(1)
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
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
        await _session
            .DidNotReceive()
            .GetSeasonAsync(Arg.Any<string>());

        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotUpdateMetadata_WhenGetCrunchyrollSeasonFails_GivenSeasonWithSeasonId()
    {
        //Arrange
        var season = SeasonFaker.GenerateWithSeasonId();
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);

        _libraryManager
            .GetItemById(season.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId])
            .Returns(Result.Fail("error"));

        //Act
        await _sut.RunAsync(season, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId]);

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

        _session
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId])
            .Returns(crunchyrollSeason);
        
        _libraryManager
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception());

        //Act & Assert
        var action = async () => await _sut.RunAsync(season, CancellationToken.None);
        await action.Should().NotThrowAsync();

        await _session
            .Received(1)
            .GetSeasonAsync(season.ProviderIds[CrunchyrollExternalKeys.SeasonId]);

        await _libraryManager
            .Received()
            .UpdateItemAsync(season, season.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
}