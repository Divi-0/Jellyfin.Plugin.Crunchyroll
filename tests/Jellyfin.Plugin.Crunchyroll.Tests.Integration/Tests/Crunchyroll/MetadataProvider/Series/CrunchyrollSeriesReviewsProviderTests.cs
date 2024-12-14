using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.MetadataProvider.Series;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollSeriesReviewsProviderTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly PluginConfiguration _config;
    private readonly CrunchyrollSeriesReviewsProvider _provider;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    
    public CrunchyrollSeriesReviewsProviderTests(WireMockFixture wireMockFixture)
    {
        _wireMockFixture = wireMockFixture;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollSeriesReviewsProvider>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;

        _config.IsFeatureReviewsEnabled = true;
    }
    
    [Fact]
    public async Task ScrapsReviews_WhenSuccessful_GivenSeries()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var seriesId = series.ProviderIds[CrunchyrollExternalKeys.SeriesId];

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        titleMetadata = titleMetadata with { CrunchyrollId = seriesId };
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);
        
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                "series",
                seriesId,
                titleMetadata.SlugTitle)
            .Replace('\\', '/');

        var waybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(crunchyrollUrl);
        var snapshotUrl = GetSnapshotUrl(waybackMachineSearchResponse, crunchyrollUrl);
        await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(snapshotUrl, _config.ArchiveOrgUrl);

        //Act
        var itemUpdateType = await _provider.FetchAsync(series, 
            new MetadataRefreshOptions(Substitute.For<IDirectoryService>()), CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);
        
        DatabaseMockHelper.ShouldHaveReviews(seriesId);
    }
    
    private string GetSnapshotUrl(IReadOnlyList<SearchResponse> waybackMachineSearchResponse, string url)
    {
        var snapshotUrl = Path.Combine(
                _config.ArchiveOrgUrl,
                "web",
                waybackMachineSearchResponse.Last().Timestamp.ToString("yyyyMMddHHmmss"),
                url)
            .Replace('\\', '/');
        return snapshotUrl;
    }
}