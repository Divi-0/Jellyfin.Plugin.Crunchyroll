using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;
using Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Client;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.WaybackMachine.Tests.Crunchyroll.PostScan;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollScanTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly CrunchyrollDatabaseFixture _crunchyrollDatabaseFixture;
    
    private readonly CrunchyrollScan _crunchyrollScan;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemRepository _itemRepository;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests(WireMockFixture wireMockFixture, CrunchyrollDatabaseFixture crunchyrollDatabaseFixture)
    {
        _wireMockFixture = wireMockFixture;
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
    public async Task ExtractsReviewsAndCommentsAndAvatarUris_WhenWaybackMachineIsEnabled_GivenCrunchyrollResponses()
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
        
        await _wireMockAdminApi.MockAvatarUriRequest($"{_config.ArchiveOrgUrl}/web/20240205003102im_/https://static.crunchyroll.com/assets/avatar/*");
        
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan([SeriesFaker.GenerateWithTitleId()]);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var series in itemList)
        {
            await _wireMockAdminApi.MockCrunchyrollSeriesResponse(series, language, 
                $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
            
            var seasons = _itemRepository.MockGetChildren(series, isSeasonIdSet: true);
            var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse(seasons, series, language);

            foreach (var seasonResponse in seasonsResponse.Data)
            {
                var season = seasons.First(x => x.IndexNumber!.Value == seasonResponse.SeasonNumber);
                var episodes = _itemRepository.MockGetChildren(season, isEpisodeIdSet: true);
                await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(episodes, seasonResponse.Id, language);

                foreach (var episode in episodes)
                {
                    var episodeCrunchyrollUrl = GetCrunchyrollUrlForEpisode(episode);
                    var episodeWaybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(episodeCrunchyrollUrl);
                    var episodeSnapshotUrl = GetSnapshotUrl(episodeWaybackMachineSearchResponse, episodeCrunchyrollUrl);
                    await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollCommentsHtml(episodeSnapshotUrl, _config.ArchiveOrgUrl);
                }
            }
            
            var crunchyrollUrl = GetCrunchyrollUrlForTitle(series);
            var waybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(crunchyrollUrl);
            var snapshotUrl = GetSnapshotUrl(waybackMachineSearchResponse, crunchyrollUrl);
            await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(snapshotUrl, _config.ArchiveOrgUrl);
        }
        
        //Act
        var progress = new Progress<double>();
        await _crunchyrollScan.Run(progress, CancellationToken.None);
        
        //Assert
        itemList.Should().AllSatisfy(series =>
        {
            DatabaseMockHelper.ShouldHaveReviews(_crunchyrollDatabaseFixture.DbFilePath, series.ProviderIds[CrunchyrollExternalKeys.Id]);

            series.Children.Should().AllSatisfy(season =>
            {
                ((Season)season).Children.Should().AllSatisfy(episode =>
                {
                    DatabaseMockHelper.ShouldHaveComments(_crunchyrollDatabaseFixture.DbFilePath, episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId]);
                });
            });
        });

        imageUris.Should().AllSatisfy(x =>
        {
            DatabaseMockHelper.ShouldHaveAvatarUri(_crunchyrollDatabaseFixture.DbFilePath, x);
        });
    }

    private string GetCrunchyrollUrlForTitle(Series series)
    {
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                "de",
                "series",
                series.ProviderIds[CrunchyrollExternalKeys.Id],
                series.ProviderIds[CrunchyrollExternalKeys.SlugTitle])
            .Replace('\\', '/');
        return crunchyrollUrl;
    }

    private string GetCrunchyrollUrlForEpisode(Episode episode)
    {
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                "de",
                "watch",
                episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId],
                episode.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle])
            .Replace('\\', '/');
        return crunchyrollUrl;
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