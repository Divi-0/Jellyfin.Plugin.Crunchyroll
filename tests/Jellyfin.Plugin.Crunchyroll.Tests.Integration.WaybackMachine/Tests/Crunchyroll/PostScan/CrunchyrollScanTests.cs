using System.Globalization;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.WaybackMachine.Tests.Crunchyroll.PostScan;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollScanTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly CrunchyrollDatabaseFixture _crunchyrollDatabaseFixture;
    
    private readonly ILibraryManager _libraryManager;
    private readonly IItemRepository _itemRepository;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests(WireMockFixture wireMockFixture, CrunchyrollDatabaseFixture crunchyrollDatabaseFixture)
    {
        _wireMockFixture = wireMockFixture;
        _crunchyrollDatabaseFixture = crunchyrollDatabaseFixture;
        
        _libraryManager =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
        _itemRepository =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<IItemRepository>();
        _mediaSourceManager =
            PluginWebApplicationFactory.Instance.Services.GetRequiredService<IMediaSourceManager>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _config.LibraryName = "/mnt/abc";
        
        _libraryManager.ClearSubstitute();
        _itemRepository.ClearSubstitute();
        _mediaSourceManager.ClearSubstitute();
    }

    [Fact]
    public async Task ExtractsReviewsAndCommentsAndAvatarUris_WhenWaybackMachineIsEnabled_GivenCrunchyrollResponses()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        
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
        
        var itemList = _libraryManager.MockCrunchyrollTitleIdScan(_itemRepository, _config.LibraryName, [SeriesFaker.GenerateWithTitleId()]);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        foreach (var series in itemList)
        {
            await _wireMockAdminApi.MockCrunchyrollSeriesResponse(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], language.Name, 
                $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
            
            var seasons = _itemRepository.MockGetChildren(series, isSeasonIdSet: true);
            var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse(seasons, series.ProviderIds[CrunchyrollExternalKeys.SeriesId], language.Name);

            foreach (var seasonResponse in seasonsResponse.Data)
            {
                var season = seasons.First(x => x.IndexNumber!.Value == seasonResponse.SeasonNumber);
                var episodes = _itemRepository.MockGetChildren(season, isEpisodeIdSet: true);
                await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(episodes, seasonResponse.Id, language.Name,
                    $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");

                foreach (var episode in episodes)
                {
                    _mediaSourceManager
                        .GetPathProtocol(episode.Path)
                        .Returns(MediaProtocol.File);
                    
                    var episodeCrunchyrollUrl = GetCrunchyrollUrlForEpisode(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId],
                        episode.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle]);
                    var episodeWaybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(episodeCrunchyrollUrl);
                    var episodeSnapshotUrl = GetSnapshotUrl(episodeWaybackMachineSearchResponse, episodeCrunchyrollUrl);
                    await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollCommentsHtml(episodeSnapshotUrl, _config.ArchiveOrgUrl);
                }
            }
            
            var crunchyrollUrl = GetCrunchyrollUrlForTitle(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
                series.ProviderIds[CrunchyrollExternalKeys.SeriesSlugTitle]);
            var waybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(crunchyrollUrl);
            var snapshotUrl = GetSnapshotUrl(waybackMachineSearchResponse, crunchyrollUrl);
            await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(snapshotUrl, _config.ArchiveOrgUrl);
        }
        
        //Act
        var progress = new Progress<double>();
        
        //Assert
        itemList.Should().AllSatisfy(series =>
        {
            DatabaseMockHelper.ShouldHaveReviews(series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);

            series.Children.Should().AllSatisfy(season =>
            {
                ((Season)season).Children.Should().AllSatisfy(episode =>
                {
                    DatabaseMockHelper.ShouldHaveComments(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId]);
                });
            });
        });

        imageUris.Should().AllSatisfy(DatabaseMockHelper.ShouldHaveAvatarUri);
    }
    
    [Fact]
    public async Task SetsCrunchyrollIdsForMovie_WhenMovieFound_GivenSeriesWithTitleIdAndChildren()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var movie = MovieFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seriesId = CrunchyrollIdFaker.Generate();
        _libraryManager.MockCrunchyrollTitleIdScanMovies(_itemRepository, _config.LibraryName, [movie]);
        
        await _wireMockAdminApi.MockRootPageAsync();
        await _wireMockAdminApi.MockAnonymousAuthAsync();
        
        _mediaSourceManager
            .GetPathProtocol(movie.Path)
            .Returns(MediaProtocol.File);

        var searchResponse = await _wireMockAdminApi.MockCrunchyrollSearchResponseForMovie(
            movie.FileNameWithoutExtension,
            language.Name,
            episodeId,
            seasonId,
            seriesId);
        
        var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(seriesId, language.Name, 
            $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
        
        var seriesRatingResponse = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(
            seriesId);

        var season = SeasonFaker.GenerateWithSeasonId();
        var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([season], seriesId, language.Name);
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _ = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([episode], seasonsResponse.Data.First().Id, language.Name,
            $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
        
        var episodeResponse = await _wireMockAdminApi.MockCrunchyrollGetEpisodeResponse(episodeId, seasonId, seriesId, 
            language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
        
        await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
            episodeResponse.Data.First().Images.Thumbnail.First().Last().Source);
        
        _mediaSourceManager
            .GetPathProtocol(episode.Path)
            .Returns(MediaProtocol.File);
                    
        var episodeCrunchyrollUrl = GetCrunchyrollUrlForEpisode(episodeId, searchResponse.Data.Last().Items.Last().SlugTitle);
        var episodeWaybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(episodeCrunchyrollUrl);
        var episodeSnapshotUrl = GetSnapshotUrl(episodeWaybackMachineSearchResponse, episodeCrunchyrollUrl);
        await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollCommentsHtml(episodeSnapshotUrl, _config.ArchiveOrgUrl);
        
        var crunchyrollUrl = GetCrunchyrollUrlForTitle(seriesId, searchResponse.Data.Last().Items.Last().EpisodeMetadata!.SeriesSlugTitle);
        var waybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(crunchyrollUrl);
        var snapshotUrl = GetSnapshotUrl(waybackMachineSearchResponse, crunchyrollUrl);
        await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(snapshotUrl, _config.ArchiveOrgUrl);
        
        //Act
        var progress = new Progress<double>();
        
        //Assert
        movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesId);
        movie.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().Be(seriesId);
        movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesSlugTitle);
        
        movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
        movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodeId);
        movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeSlugTitle);
        
        movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeasonId);
        movie.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(seasonId);
        
        DatabaseMockHelper.ShouldHaveMetadata(seriesId, seriesResponse, seriesRatingResponse);
        DatabaseMockHelper.ShouldHaveReviews(seriesId);
        DatabaseMockHelper.ShouldHaveComments(episodeId);
    }

    private string GetCrunchyrollUrlForTitle(string seriesId, string seriesSlugTitle)
    {
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                "series",
                seriesId,
                seriesSlugTitle)
            .Replace('\\', '/');
        return crunchyrollUrl;
    }

    private string GetCrunchyrollUrlForEpisode(string episodeId, string episodeSlugTitle)
    {
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                "watch",
                episodeId,
                episodeSlugTitle)
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