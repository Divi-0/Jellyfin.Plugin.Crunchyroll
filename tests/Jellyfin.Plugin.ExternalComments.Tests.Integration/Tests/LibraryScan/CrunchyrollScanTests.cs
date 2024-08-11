using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Tests.LibraryScan;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollScanTests
{
    private readonly CrunchyrollDatabaseFixture _databaseFixture;
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollScan _crunchyrollScan;
    private readonly ILibraryManager _libraryManager;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly IItemRepository _itemRepository;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests(WireMockFixture wireMockFixture, CrunchyrollDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        _fixture = new Fixture();
        
        _crunchyrollScan =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollScan>();
        _libraryManager =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
        _itemRepository =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<IItemRepository>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
        _config = ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
    }

    [Fact]
    public async Task SetsTitleIdsAndMetadata_GivenCrunchyrollResponses()
    {
        //Arrange
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan();
        const string language = "de-DE";
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var item in itemList)
        { 
            var searchResponse = await _wireMockAdminApi.MockCrunchyrollSearchResponse(item.Name, language);

            var titles = searchResponse.Data.Where(x => x.Type == "series").SelectMany(x => x.Items);
            foreach (var title in titles)
            {
                var seasonResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse(title.Id, language);

                foreach (var season in seasonResponse.Data)
                {
                    await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(season.Id, language);
                }
            }
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
            
            DatabaseMockHelper.ShouldHaveMetadata(_databaseFixture.DbFilePath, x.ProviderIds[CrunchyrollExternalKeys.Id]);
        });
    }

    [Fact]
    public async Task SetsUniqueSeasonIds_WhenSeasonsWithTheSameSeasonNumberFound_GivenCrunchyrollResponses()
    {
        //Arrange
        const int duplicateSeasonNumber = 532;
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan();
        const string language = "de-DE";
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var item in itemList)
        { 
            _itemRepository.MockGetChildren(item);
            
            var searchResponse = await _wireMockAdminApi.MockCrunchyrollSearchResponse(item.Name, language);

            var titles = searchResponse.Data.Where(x => x.Type == "series").SelectMany(x => x.Items);
            foreach (var title in titles)
            {
                var seasonResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse(title.Id, language);

                foreach (var season in seasonResponse.Data)
                {
                    await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(season.Id, language);
                }
            }
        }

        //Last Item
        var children = _itemRepository.MockGetChildren(itemList.Last());
        children[0].IndexNumber = duplicateSeasonNumber;
        children[1].IndexNumber = duplicateSeasonNumber;
            
        var lastItemSearchResponse = await _wireMockAdminApi.MockCrunchyrollSearchResponse(itemList.Last().Name, language);

        var duplicateSeasonsIds = new List<string>();
        
        var lastItemTitles = lastItemSearchResponse.Data
            .Where(x => x.Type == "series")
            .SelectMany(x => x.Items)
            .Where(x => x.Title == itemList.Last().Name);
        
        foreach (var title in lastItemTitles)
        {
            var seasonResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponseWithDuplicate(title.Id, language, duplicateSeasonNumber);

            foreach (var season in seasonResponse.Data)
            {
                await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(season.Id, language);

                if (season.SeasonNumber == duplicateSeasonNumber)
                {
                    duplicateSeasonsIds.Add(season.Id);
                }
            }
        }
        
        //Act
        var progress = new Progress<double>();
        await _crunchyrollScan.Run(progress, CancellationToken.None);
        
        //Assert
        var duplicateChildren = ((Folder)itemList.Last()).Children
            .Where(x => x.IndexNumber == duplicateSeasonNumber)
            .ToList();
        
        duplicateChildren[0].ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(duplicateSeasonsIds[0]);
        duplicateChildren[1].ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(duplicateSeasonsIds[1]);
    }

    [Fact]
    public async Task SetsEmptyCrunchyrollTitleId_GivenCrunchyrollResponses()
    {
        //Arrange
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan();
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var item in itemList)
        {
            await _wireMockAdminApi.MockCrunchyrollSearchResponseNoMatch(item.Name, "de-DE");
        }
        
        //Act
        var progress = new Progress<double>();
        await _crunchyrollScan.Run(progress, CancellationToken.None);
        
        //Assert
        itemList.Should().AllSatisfy(x =>
        {
            x.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.Id);
            x.ProviderIds[CrunchyrollExternalKeys.Id].Should().BeEmpty();
        });
    }
}