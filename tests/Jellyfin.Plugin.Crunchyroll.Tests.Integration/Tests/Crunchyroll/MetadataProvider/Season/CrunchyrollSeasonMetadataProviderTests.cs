using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.MetadataProvider.Season;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollSeasonMetadataProviderTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly PluginConfiguration _config;
    private readonly CrunchyrollSeasonProvider _provider;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    
    public CrunchyrollSeasonMetadataProviderTests(WireMockFixture wireMockFixture)
    {
        _wireMockFixture = wireMockFixture;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollSeasonProvider>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
    }
    
    [Fact]
    public async Task StoresSeasonsAndSetsSeasonId_WhenSuccessful_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var language = seasonInfo.GetPreferredMetadataCultureInfo();

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        titleMetadata = titleMetadata with { CrunchyrollId = seriesId };
        
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();

        var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([seasonInfo], seriesId,
            language.Name);

        var seasonResponse = seasonsResponse.Data.First();
        
        //Act
        var metadataResult = await _provider.GetMetadata(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var seasonWithNewMetadata = metadataResult.Item;

        seasonWithNewMetadata.Name.Should().Be($"S{seasonResponse.SeasonSequenceNumber}: {seasonResponse.Title}");

        seasonWithNewMetadata.ProviderIds.Should().Contain(
            new KeyValuePair<string, string>(CrunchyrollExternalKeys.SeasonId, seasonResponse.Id));

        await DatabaseMockHelper.ShouldHaveSeasons(seriesId, seasonsResponse);
    }
}