using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteEpisodeJellyfinDataTaskTests
{
    private readonly ILibraryManager _libraryManager;
    private readonly IOverwriteEpisodeJellyfinDataTaskSession _session;
    private readonly ISetEpisodeThumbnail _setEpisodeThumbnail;
    private readonly OverwriteEpisodeJellyfinDataTask _sut;
    
    public OverwriteEpisodeJellyfinDataTaskTests()
    {
        _libraryManager = MockHelper.LibraryManager;
        _session = Substitute.For<IOverwriteEpisodeJellyfinDataTaskSession>();
        var logger = Substitute.For<ILogger<OverwriteEpisodeJellyfinDataTask>>();
        _setEpisodeThumbnail = Substitute.For<ISetEpisodeThumbnail>();
        
        _sut = new OverwriteEpisodeJellyfinDataTask(logger, _libraryManager, _session, _setEpisodeThumbnail);
    }

    [Fact]
    public async Task SetsTitleDescriptionAndThumbnail_WhenSuccessful_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>());
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Value.Should().Be(int.Parse(crunchyrollEpisode.EpisodeNumber));
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("432", 432)]
    [InlineData("FMI1", 1)]
    [InlineData("FMI2", 2)]
    public async Task SetsIndexNumberAndTitleWithEpisodeNumber_WhenIndexNumberOfJellyfinEpisodeIsNull_GivenEpisodeWithEpisodeId(
        string episodeIdentifier, int? expectedIndexNumber)
    {
        //Arrange
        var season = SeasonFaker.Generate();
        var episode = EpisodeFaker.GenerateWithEpisodeId(season);
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        episode.IndexNumber = null;
        
        if (!string.IsNullOrWhiteSpace(episodeIdentifier))
        {
            crunchyrollEpisode = crunchyrollEpisode with
            {
                EpisodeNumber = episodeIdentifier,
                SequenceNumber = Convert.ToDouble(expectedIndexNumber)
            };
        }
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns(season);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>());

        episode.Name.Should().Be($"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}");

        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Should().Be(expectedIndexNumber == 0 
            ? int.Parse(crunchyrollEpisode.EpisodeNumber) 
            : expectedIndexNumber);
        episode.AirsBeforeEpisodeNumber.Should().BeNull();
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetsNameAndDescription_WhenIndexNumberOfJellyfinEpisodeIsNullAndCrunchyrollEpisodeNumberIsEmpty_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var season = SeasonFaker.Generate();
        var episode = EpisodeFaker.GenerateWithEpisodeId(season);
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        episode.IndexNumber = null;
        
        crunchyrollEpisode = crunchyrollEpisode with { EpisodeNumber = string.Empty };
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns(season);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>());

        episode.Name.Should().Be(crunchyrollEpisode.Title);

        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber.Should().BeNull();
        episode.AirsBeforeEpisodeNumber.Should().BeNull();
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetsProviderIdDecimalEpisodeNumberAndSetsAirsBefore_WhenIndexNumberOfJellyfinEpisodeIsNullAndCrunchyrollEpisodeNumberIsDecimal_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var season = SeasonFaker.Generate();
        var episode = EpisodeFaker.GenerateWithEpisodeId(season);
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        episode.IndexNumber = null;
        
        crunchyrollEpisode = crunchyrollEpisode with
        {
            SequenceNumber = Random.Shared.Next(1, int.MaxValue - 1) + 0.5
        };
        
        _libraryManager
            .GetItemById(episode.SeasonId)
            .Returns(season);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>());

        episode.Name.Should().Be($"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}");
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Should().Be(null);
        episode.ProviderIds[CrunchyrollExternalKeys.EpisodeDecimalEpisodeNumber]
            .Should().Be(crunchyrollEpisode.SequenceNumber.ToString("0.0"));
        episode.AirsBeforeEpisodeNumber.Should().Be(Convert.ToInt32(crunchyrollEpisode.SequenceNumber + 0.5));
        episode.AirsBeforeSeasonNumber.Should().Be(season.IndexNumber);
        episode.ParentIndexNumber.Should()
            .Be(0, "Specials have to be in Season 0");
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotUpdateEpisode_WhenGetEpisodeFails_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode);
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _session
            .GetEpisodeAsync(crunchyrollEpisode.Id)
            .Returns(Result.Fail("error"));

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _session
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.Id);
        
        await _setEpisodeThumbnail
            .DidNotReceive()
            .GetAndSetThumbnailAsync(episode, crunchyrollEpisode.Thumbnail, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
}