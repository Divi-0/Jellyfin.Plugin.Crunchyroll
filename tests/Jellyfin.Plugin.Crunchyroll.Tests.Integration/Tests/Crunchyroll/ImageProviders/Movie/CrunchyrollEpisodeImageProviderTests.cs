using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.ImageProviders.Movie;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollMovieImageProviderTests
{
    private readonly CrunchyrollMovieImageProvider _provider;
    
    public CrunchyrollMovieImageProviderTests()
    {
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollMovieImageProvider>();
    }
    
    [Fact]
    public async Task ReturnsThumb_WhenSuccessful_GivenEpisode()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var episodeId = movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId];

        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        crunchyrollEpisode = crunchyrollEpisode with { CrunchyrollId = episodeId };
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(crunchyrollEpisode);
        
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        titleMetadata.Seasons.Add(season);
        
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);

        //Act
        var remoteImageInfos = await _provider.GetImages(movie, CancellationToken.None);

        //Assert
        var thumbnail = JsonSerializer.Deserialize<ImageSource>(crunchyrollEpisode.Thumbnail)!;
        
        var imageInfos = remoteImageInfos.ToArray();
        imageInfos.Should().HaveCount(1);
        imageInfos.Should().Contain(x =>
            x.Url == thumbnail.Uri &&
            x.Width == thumbnail.Width &&
            x.Height == thumbnail.Height &&
            x.Type == ImageType.Thumb);
    }
}