using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class OverwriteEpisodeJellyfinDataTaskTests
{
    private readonly ILibraryManager _libraryManager;
    private readonly IOverwriteEpisodeJellyfinDataTaskRepository _repository;
    private readonly ISetEpisodeThumbnail _setEpisodeThumbnail;
    private readonly PluginConfiguration _config;
    
    private readonly OverwriteEpisodeJellyfinDataTask _sut;
    
    public OverwriteEpisodeJellyfinDataTaskTests()
    {
        _libraryManager = MockHelper.LibraryManager;
        _repository = Substitute.For<IOverwriteEpisodeJellyfinDataTaskRepository>();
        var logger = Substitute.For<ILogger<OverwriteEpisodeJellyfinDataTask>>();
        _setEpisodeThumbnail = Substitute.For<ISetEpisodeThumbnail>();
        _config = new PluginConfiguration
        {
            IsFeatureEpisodeTitleEnabled = true,
            IsFeatureEpisodeDescriptionEnabled = true,
            IsFeatureEpisodeThumbnailImageEnabled = true,
            IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = true
        };
        
        _sut = new OverwriteEpisodeJellyfinDataTask(logger, _libraryManager, _repository, _setEpisodeThumbnail,
            _config);
    }

    [Fact]
    public async Task SetsTitleDescriptionAndThumbnail_WhenSuccessful_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Value.Should().Be(int.Parse(crunchyrollEpisode.EpisodeNumber));
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetsTitleNameWithoutBrackets_WhenTitleHasBracketsAtStart_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var title = crunchyrollEpisode.Title;
        crunchyrollEpisode = crunchyrollEpisode with { Title = $"(OMU) {crunchyrollEpisode.Title}" };
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        episode.Name.Should().Be(title);
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
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
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

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());

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
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        episode.IndexNumber = null;
        
        crunchyrollEpisode = crunchyrollEpisode with { EpisodeNumber = string.Empty };
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns(season);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());

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
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        episode.IndexNumber = null;
        
        crunchyrollEpisode = crunchyrollEpisode with
        {
            SequenceNumber = Random.Shared.Next(1, int.MaxValue - 1) + 0.5
        };
        
        _libraryManager
            .GetItemById(episode.SeasonId)
            .Returns(season);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());

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
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
        
        await _setEpisodeThumbnail
            .DidNotReceive()
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsItem_WhenRepositoryGetEpisodeReturnsNull_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Episode?>(null));
        

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        await _setEpisodeThumbnail
            .DidNotReceive()
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>());
        
        await _libraryManager
            .DidNotReceive()
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotDownloadAndSetThumbnail_WhenFeatureEpisodeThumbnailIsDisabled_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;

        _config.IsFeatureEpisodeThumbnailImageEnabled = false;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        await _setEpisodeThumbnail
            .DidNotReceive()
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Value.Should().Be(int.Parse(crunchyrollEpisode.EpisodeNumber));
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateTitle_WhenFeatureEpisodeTitleIsDisabled_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;

        _config.IsFeatureEpisodeTitleEnabled = false;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        episode.Name.Should().NotBe(crunchyrollEpisode.Title, "feature is disabled");
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Value.Should().Be(int.Parse(crunchyrollEpisode.EpisodeNumber));
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateOverview_WhenFeatureEpisodeDescriptionIsDisabled_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;

        _config.IsFeatureEpisodeDescriptionEnabled = false;
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns((BaseItem?)null);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());
        
        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().NotBe(crunchyrollEpisode.Description, "feature is disabled");
        episode.IndexNumber!.Value.Should().Be(int.Parse(crunchyrollEpisode.EpisodeNumber));
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, episode.DisplayParent, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotSetIndexNumberAndTitleWithEpisodeNumber_WhenIndexNumberOfJellyfinEpisodeIsNullAndFeatureEpisodeIncludeSpecialsInNormalSeasonsIsDisabled_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var season = SeasonFaker.Generate();
        var episode = EpisodeFaker.GenerateWithEpisodeId(season);
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(episode: episode);
        var thumbnailImageSource = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        episode.IndexNumber = null;

        _config.IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = false;

        const string episodeIdentifier = "FMI3";
        const int expectedIndexNumber = 3;
        crunchyrollEpisode = crunchyrollEpisode with
        {
            EpisodeNumber = episodeIdentifier,
            SequenceNumber = Convert.ToDouble(expectedIndexNumber)
        };
        
        _libraryManager
            .GetItemById(episode.ParentId)
            .Returns(season);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        _setEpisodeThumbnail
            .GetAndSetThumbnailAsync(episode, thumbnailImageSource, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        await _sut.RunAsync(episode, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
        
        await _setEpisodeThumbnail
            .Received(1)
            .GetAndSetThumbnailAsync(episode, Arg.Is<ImageSource>(x => 
                x.Uri == thumbnailImageSource.Uri &&
                x.Height == thumbnailImageSource.Height &&
                x.Width == thumbnailImageSource.Width), Arg.Any<CancellationToken>());

        episode.Name.Should().Be(crunchyrollEpisode.Title);
        episode.Overview.Should().Be(crunchyrollEpisode.Description);
        episode.IndexNumber!.Should().NotBe(expectedIndexNumber);
        episode.AirsBeforeEpisodeNumber.Should().BeNull();
        
        await _libraryManager
            .Received(1)
            .UpdateItemAsync(episode, season, ItemUpdateType.MetadataEdit, Arg.Any<CancellationToken>());
    }
}