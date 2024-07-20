using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Tests.LibraryScan;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollScanTests
{
    private readonly Fixture _fixture;
    
    private readonly CrunchyrollScan _crunchyrollScan;
    private readonly ILibraryManager _libraryManager;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests(WireMockFixture wireMockFixture)
    {
        _fixture = new Fixture();
        
        _crunchyrollScan =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<CrunchyrollScan>();
        _libraryManager =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
        _config = ExternalCommentsPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
    }

    [Fact]
    public async Task SetsTitleIdsWithNoEmptyValues_GivenCrunchyrollResponses()
    {
        //Arrange
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan();
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var item in itemList)
        {
            var searchResponse = await _wireMockAdminApi.MockCrunchyrollSearchResponse(item.Name, "de-DE");

            foreach (var searchData in searchResponse.Data.Where(x => x.Type == "series"))
            {
                foreach (var dataItem in searchData.Items)
                {
                    var availabilityResponse = await _wireMockAdminApi.MockWaybackMachineAvailabilityResponse(dataItem.Id, dataItem.SlugTitle, _config.CrunchyrollUrl);
                    
                    await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(availabilityResponse.ArchivedSnapshots.Closest.Url);
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
        });
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