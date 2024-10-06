using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WireMock.Client;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Tests.PostScan;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollScanTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly CrunchyrollDatabaseFixture _databaseFixture;
    
    private readonly CrunchyrollScan _crunchyrollScan;
    private readonly ILibraryManager _libraryManager;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly IItemRepository _itemRepository;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests(WireMockFixture wireMockFixture, CrunchyrollDatabaseFixture databaseFixture)
    {
        _wireMockFixture = wireMockFixture;
        _databaseFixture = databaseFixture;
        
        _crunchyrollScan =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollScan>();
        _libraryManager =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
        _itemRepository =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<IItemRepository>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
        _config = ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();

        _config.IsWaybackMachineEnabled = false;
    }

    [Fact]
    public async Task SetsTitleIds_GivenCrunchyrollResponses()
    {
        //Arrange
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan();
        const string language = "de-DE";
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var item in itemList)
        { 
            _itemRepository.MockGetChildrenEmpty(item);
            await _wireMockAdminApi.MockCrunchyrollSearchResponse(item.Name, language);
        }
        
        //Act
        var progress = new Progress<double>();
        await _crunchyrollScan.Run(progress, CancellationToken.None);
        
        //Assert
        itemList.Should().AllSatisfy(x =>
        {
            x.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.Id);
            x.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SlugTitle);
            x.ProviderIds[CrunchyrollExternalKeys.Id].Should().NotBeEmpty();
            x.ProviderIds[CrunchyrollExternalKeys.SlugTitle].Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task SetsSeasonIdsAndEpisodeIdsAndScrapsTitleMetadata_WhenTitlePostScanTasksAreCalled_GivenSeriesWithTitleIdAndChildren()
    {
        //Arrange
        const string language = "de-DE";
        var seriesItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => SeriesFaker.GenerateWithTitleId())
            .ToList();
        
        _libraryManager.MockCrunchyrollTitleIdScan(seriesItems);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();

        var seriesResponses = new Dictionary<Series, CrunchyrollSeriesContentItem>();
        foreach (var series in seriesItems)
        {
            var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(series, language, 
                $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
            seriesResponses.Add(series, seriesResponse);
            
            await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
                seriesResponse.Images.PosterTall.First().Last().Source);            
            
            await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
                seriesResponse.Images.PosterWide.First().Last().Source);
            
            var seasons = _itemRepository.MockGetChildren(series);
            var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse(seasons, series, language);

            foreach (var seasonResponse in seasonsResponse.Data)
            {
                var season = seasons.First(x => x.IndexNumber!.Value == seasonResponse.SeasonNumber);
                var episodes = _itemRepository.MockGetChildren(season);
                await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(episodes, seasonResponse.Id, language);
            }
        }
        
        //Act
        var progress = new Progress<double>();
        await _crunchyrollScan.Run(progress, CancellationToken.None);
        
        //Assert
        seriesItems.Should().AllSatisfy(series =>
        {
            DatabaseMockHelper.ShouldHaveMetadata(_databaseFixture.DbFilePath, 
                series.ProviderIds[CrunchyrollExternalKeys.Id],
                seriesResponses[series]);
            
            foreach (var season in series.Children)
            {
                season.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeasonId);
                season.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().NotBeEmpty();
                
                foreach (var episode in ((Season)season).Children)
                {
                    episode.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
                    episode.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeSlugTitle);
                    episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().NotBeEmpty();
                    episode.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle].Should().NotBeEmpty();
                }
            }
        });
    }
}