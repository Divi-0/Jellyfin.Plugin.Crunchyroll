using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
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
}