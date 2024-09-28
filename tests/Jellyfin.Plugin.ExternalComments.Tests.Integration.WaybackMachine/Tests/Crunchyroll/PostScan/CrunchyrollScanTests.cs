using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.WaybackMachine.Tests.Crunchyroll.PostScan;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollScanTests
{
    private readonly CrunchyrollDatabaseFixture _crunchyrollDatabaseFixture;
    
    private readonly CrunchyrollScan _crunchyrollScan;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemRepository _itemRepository;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests(WireMockFixture wireMockFixture, CrunchyrollDatabaseFixture crunchyrollDatabaseFixture)
    {
        _crunchyrollDatabaseFixture = crunchyrollDatabaseFixture;
        
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
    public async Task ExtractsReviewsAndAvatarUris_WhenWaybackMachineIsEnabled_GivenCrunchyrollResponses()
    {
        //Arrange
        const string language = "de-DE";
        
        //uri from ResourcesHtml
        var imageUris = new []
        {
            $"{_config.ArchiveOrgUrl}/web/20240707123516im_/https://static.crunchyroll.com/assets/avatar/170x170/1010-mitrasphere-upa.png",
            $"{_config.ArchiveOrgUrl}/web/20240707123516im_/https://static.crunchyroll.com/assets/avatar/170x170/13-tower-of-god-rak.png",
            $"{_config.ArchiveOrgUrl}/web/20240707123516im_/https://static.crunchyroll.com/assets/avatar/170x170/21-bananya-bananya.png",
            $"{_config.ArchiveOrgUrl}/web/20240707123516im_/https://static.crunchyroll.com/assets/avatar/170x170/11-tower-of-god-bam.png",
            $"{_config.ArchiveOrgUrl}/web/20240707123516im_/https://static.crunchyroll.com/assets/avatar/170x170/chainsawman-aki.png"
        };
        
        foreach (var uri in imageUris)
        {
            await _wireMockAdminApi.MockAvatarUriRequest(uri);
        }
        
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan([SeriesFaker.GenerateWithTitleId()]);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var series in itemList)
        {
            _itemRepository.MockGetChildrenEmpty(series);
            
            var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([SeasonFaker.Generate(series)], language);

            foreach (var seasonResponse in seasonsResponse.Data)
            {
                await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([], seasonResponse.Id, language);
            }
            
            var crunchyrollUrl = Path.Combine(
                    _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                    "de",
                    "series",
                    series.ProviderIds[CrunchyrollExternalKeys.Id],
                    series.ProviderIds[CrunchyrollExternalKeys.SlugTitle])
                .Replace('\\', '/');
                    
            var waybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(crunchyrollUrl);
                    
            var snapshotUrl = Path.Combine(
                    _config.ArchiveOrgUrl,
                    "web",
                    waybackMachineSearchResponse.Last().Timestamp.ToString("yyyyMMddHHmmss"),
                    crunchyrollUrl)
                .Replace('\\', '/');
                    
            await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(snapshotUrl, _config.ArchiveOrgUrl);
        }
        
        //Act
        var progress = new Progress<double>();
        await _crunchyrollScan.Run(progress, CancellationToken.None);
        
        //Assert
        itemList.Should().AllSatisfy(x =>
        {
            DatabaseMockHelper.ShouldHaveReviews(_crunchyrollDatabaseFixture.DbFilePath, x.ProviderIds[CrunchyrollExternalKeys.Id]);
        });

        imageUris.Should().AllSatisfy(x =>
        {
            DatabaseMockHelper.ShouldHaveAvatarUri(_crunchyrollDatabaseFixture.DbFilePath, x);
        });
    }
}