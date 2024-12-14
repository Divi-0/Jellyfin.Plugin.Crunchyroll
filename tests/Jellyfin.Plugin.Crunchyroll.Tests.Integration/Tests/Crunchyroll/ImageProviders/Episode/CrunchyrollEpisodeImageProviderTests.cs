using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.ImageProviders.Episode;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollEpisodeImageProviderTests
{
    private readonly CrunchyrollEpisodeImageProvider _provider;
    
    public CrunchyrollEpisodeImageProviderTests()
    {
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollEpisodeImageProvider>();
    }
    
    [Fact]
    public async Task ReturnsPrimaryAndThumb_WhenSuccessful_GivenEpisode()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId];

        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        crunchyrollEpisode = crunchyrollEpisode with { CrunchyrollId = episodeId };
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(crunchyrollEpisode);
        
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        titleMetadata.Seasons.Add(season);
        
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);

        //Act
        var remoteImageInfos = await _provider.GetImages(episode, CancellationToken.None);

        //Assert
        var thumbnail = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        var imageInfos = remoteImageInfos.ToArray();
        imageInfos.Should().HaveCount(2);
        imageInfos.Should().Contain(x =>
            x.Url == thumbnail.Uri &&
            x.Width == thumbnail.Width &&
            x.Height == thumbnail.Height &&
            x.Type == ImageType.Primary);
        
        imageInfos.Should().Contain(x =>
            x.Url == thumbnail.Uri &&
            x.Width == thumbnail.Width &&
            x.Height == thumbnail.Height &&
            x.Type == ImageType.Thumb);
    }
}