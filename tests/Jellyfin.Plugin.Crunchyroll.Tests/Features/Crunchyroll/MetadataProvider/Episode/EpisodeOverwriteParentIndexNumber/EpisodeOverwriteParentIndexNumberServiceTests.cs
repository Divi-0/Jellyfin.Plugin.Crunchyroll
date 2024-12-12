using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;

public class EpisodeOverwriteParentIndexNumberServiceTests
{
    private readonly EpisodeOverwriteParentIndexNumberService _sut;
    private readonly PluginConfiguration _config;

    public EpisodeOverwriteParentIndexNumberServiceTests()
    {
        _config = new PluginConfiguration
        {
            IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = true
        };
        _sut = new EpisodeOverwriteParentIndexNumberService(_config);
    }

    [Fact]
    public async Task SetsParentIndexNumber_WhenRun_GivenEpisodeWithAirsBefore()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        episode.AirsBeforeEpisodeNumber = Random.Shared.Next(1, int.MaxValue);
        episode.AirsBeforeSeasonNumber = Random.Shared.Next(1, int.MaxValue);
        episode.ParentIndexNumber = Random.Shared.Next(1, int.MaxValue);

        //Act
        var itemUpdateType = await _sut.SetParentIndexAsync(episode);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.MetadataEdit);
        episode.ParentIndexNumber.Should().Be(0);
    }

    [Fact]
    public async Task DoesNotSetParentIndexNumber_WhenRun_GivenEpisodeWithoutAirsBefore()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        episode.ParentIndexNumber = Random.Shared.Next(1, int.MaxValue);

        //Act
        var itemUpdateType = await _sut.SetParentIndexAsync(episode);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);
        episode.ParentIndexNumber.Should().NotBe(0);
    }

    [Fact]
    public async Task DoesNotSetParentIndexNumber_WhenFeatureIsDisabled_GivenEpisodeWithAirsBefore()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        episode.AirsBeforeEpisodeNumber = Random.Shared.Next(1, int.MaxValue);
        episode.AirsBeforeSeasonNumber = Random.Shared.Next(1, int.MaxValue);
        episode.ParentIndexNumber = Random.Shared.Next(1, int.MaxValue);

        _config.IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = false;

        //Act
        var itemUpdateType = await _sut.SetParentIndexAsync(episode);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);
        episode.ParentIndexNumber.Should().NotBe(0);
    }
    
    [Fact]
    public async Task DoesNotSetParentIndexNumber_WhenRun_GivenEpisodeWithParentIndexZero()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        episode.AirsBeforeEpisodeNumber = Random.Shared.Next(1, int.MaxValue);
        episode.AirsBeforeSeasonNumber = Random.Shared.Next(1, int.MaxValue);
        episode.ParentIndexNumber = 0;

        //Act
        var itemUpdateType = await _sut.SetParentIndexAsync(episode);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);
        episode.ParentIndexNumber.Should().Be(0);
    }
}