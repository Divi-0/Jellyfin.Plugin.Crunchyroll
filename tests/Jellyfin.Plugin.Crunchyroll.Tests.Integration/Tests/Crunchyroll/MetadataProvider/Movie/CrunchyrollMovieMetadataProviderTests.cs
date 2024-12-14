using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.MetadataProvider.Movie;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollMovieMetadataProviderTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly PluginConfiguration _config;
    private readonly CrunchyrollMovieProvider _provider;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    
    public CrunchyrollMovieMetadataProviderTests(WireMockFixture wireMockFixture)
    {
        _wireMockFixture = wireMockFixture;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _provider = PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollMovieProvider>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
    }
    
    [Fact]
    public async Task StoresEpisodeAndSetsEpisodeId_WhenSuccessful_GivenEpisodeInfo()
    {
        //Arrange
        var movieInfo = MovieInfoFaker.Generate();
        var language = movieInfo.GetPreferredMetadataCultureInfo();
        var seriesId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var fileName = Path.GetFileNameWithoutExtension(movieInfo.Path);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();

        await _wireMockAdminApi.MockCrunchyrollSearchResponseForMovie(fileName, language.Name,
            episodeId, seasonId, seriesId);
        
        var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(seriesId, 
            language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
        
        var seriesRatingResponse = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(seriesId);

        var seasonInfo = SeasonInfoFaker.Generate();
        seasonInfo.ProviderIds.Add(CrunchyrollExternalKeys.SeasonId, seasonId);
        var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([seasonInfo], seriesId,
            language.Name);
        
        var episodeResponse = await _wireMockAdminApi.MockCrunchyrollGetEpisodeResponse(episodeId, seasonId, seriesId, 
             language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");

        var episodeItem = episodeResponse.Data.First();
        
        //Act
        var metadataResult = await _provider.GetMetadata(movieInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        var movieWithNewMetadata = metadataResult.Item;

        movieWithNewMetadata.Name.Should().Be(episodeItem.Title);
        movieWithNewMetadata.Overview.Should().Be(episodeItem.Description);
        movieWithNewMetadata.Studios.Should().Contain(seriesResponse.ContentProvider);

        movieWithNewMetadata.ProviderIds.Should().Contain(
            new KeyValuePair<string, string>(CrunchyrollExternalKeys.SeriesId, seriesId));
        movieWithNewMetadata.ProviderIds.Should().Contain(
            new KeyValuePair<string, string>(CrunchyrollExternalKeys.SeasonId, seasonId));
        movieWithNewMetadata.ProviderIds.Should().Contain(
            new KeyValuePair<string, string>(CrunchyrollExternalKeys.EpisodeId, episodeId));

        await DatabaseMockHelper.ShouldHaveMovieTitleMetadata(seriesId, seasonId, episodeId,
            seriesResponse, seriesRatingResponse, seasonsResponse, episodeResponse);
    }
}