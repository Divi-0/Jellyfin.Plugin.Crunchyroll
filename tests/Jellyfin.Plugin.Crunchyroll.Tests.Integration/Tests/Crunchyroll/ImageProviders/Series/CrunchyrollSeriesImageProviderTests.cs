using System.Text.Json;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.ImageProviders.Series;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollSeriesImageProviderTests
{
    private readonly CrunchyrollSeriesImageProvider _provider;
    
    public CrunchyrollSeriesImageProviderTests()
    {
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollSeriesImageProvider>();
    }
    
    [Fact]
    public async Task ReturnsPrimaryAndBackdrop_WhenSuccessful_GivenSeries()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var seriesId = series.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        titleMetadata = titleMetadata with { CrunchyrollId = seriesId };
        
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);

        //Act
        var remoteImageInfos = await _provider.GetImages(series, CancellationToken.None);

        //Assert
        var posterTall = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterTall)!;
        var posterWide = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterWide)!;
        
        var imageInfos = remoteImageInfos.ToArray();
        imageInfos.Should().HaveCount(2);
        imageInfos.Should().Contain(x =>
            x.Url == posterTall.Uri &&
            x.Width == posterTall.Width &&
            x.Height == posterTall.Height &&
            x.Type == ImageType.Primary);
        
        imageInfos.Should().Contain(x =>
            x.Url == posterWide.Uri &&
            x.Width == posterWide.Width &&
            x.Height == posterWide.Height &&
            x.Type == ImageType.Backdrop);
    }
}