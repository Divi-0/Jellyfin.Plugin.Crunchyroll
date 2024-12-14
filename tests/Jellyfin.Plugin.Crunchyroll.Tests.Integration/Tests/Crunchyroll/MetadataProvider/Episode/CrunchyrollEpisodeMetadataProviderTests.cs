using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.MetadataProvider.Episode;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollEpisodeMetadataProviderTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly PluginConfiguration _config;
    private readonly CrunchyrollEpisodeProvider _provider;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    
    public CrunchyrollEpisodeMetadataProviderTests(WireMockFixture wireMockFixture)
    {
        _wireMockFixture = wireMockFixture;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollEpisodeProvider>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
    }
    
    [Fact]
    public async Task StoresEpisodeAndSetsEpisodeId_WhenSuccessful_GivenEpisodeInfo()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var seriesId = episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var seasonId = episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId];
        var language = episodeInfo.GetPreferredMetadataCultureInfo();

        var season = CrunchyrollSeasonFaker.Generate();
        season = season with { CrunchyrollId = seasonId };
        
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        titleMetadata = titleMetadata with { CrunchyrollId = seriesId };
        
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();

        var episodesResponse = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([episodeInfo],
            seasonId, language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");

        var episodeResponse = episodesResponse.Data.First();
        
        //Act
        var metadataResult = await _provider.GetMetadata(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var episodeWithNewMetadata = metadataResult.Item;

        episodeWithNewMetadata.Name.Should().Be(episodeResponse.Title);
        episodeWithNewMetadata.Overview.Should().Be(episodeResponse.Description);

        episodeWithNewMetadata.ProviderIds.Should().Contain(
            new KeyValuePair<string, string>(CrunchyrollExternalKeys.EpisodeId, episodeResponse.Id));

        await DatabaseMockHelper.ShouldHaveEpisodes(seasonId, episodesResponse);
    }
}