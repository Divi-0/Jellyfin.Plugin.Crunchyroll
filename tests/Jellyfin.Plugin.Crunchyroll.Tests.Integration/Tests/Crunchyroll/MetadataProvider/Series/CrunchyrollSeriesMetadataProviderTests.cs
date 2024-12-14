using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.MetadataProvider.Series;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollSeriesMetadataProviderTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly PluginConfiguration _config;
    private readonly CrunchyrollSeriesProvider _provider;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    
    public CrunchyrollSeriesMetadataProviderTests(WireMockFixture wireMockFixture)
    {
        _wireMockFixture = wireMockFixture;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollSeriesProvider>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
    }

    [Fact]
    public async Task StoresTitleMetadataAndSetsSeriesId_WhenSuccessful_GivenSeriesInfo()
    {
        //Arrange
        var seriesInfo = SeriesInfoFaker.Generate();
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = seriesInfo.GetPreferredMetadataCultureInfo();
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        _ = await _wireMockAdminApi.MockCrunchyrollSearchResponse(seriesId, seriesInfo);
        
        var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(seriesId, 
            language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
        
        var seriesRatingResponse = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(seriesId);

        //Act
        var metadataResult = await _provider.GetMetadata(seriesInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var seriesWithNewMetadata = metadataResult.Item;

        seriesWithNewMetadata.Name.Should().Be(seriesResponse.Title);
        seriesWithNewMetadata.Overview.Should().Be(seriesResponse.Description);
        seriesWithNewMetadata.Studios.Should().Contain(seriesResponse.ContentProvider);

        seriesWithNewMetadata.ProviderIds.Should().Contain(
            new KeyValuePair<string, string>(CrunchyrollExternalKeys.SeriesId, seriesId));
        
        await DatabaseMockHelper.ShouldHaveMetadata(seriesId, seriesResponse, seriesRatingResponse);
    }
}