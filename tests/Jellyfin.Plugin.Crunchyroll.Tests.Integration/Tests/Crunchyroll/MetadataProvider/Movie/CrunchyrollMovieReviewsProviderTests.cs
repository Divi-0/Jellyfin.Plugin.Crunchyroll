using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WireMock.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.Crunchyroll.MetadataProvider.Movie;

[Collection(CollectionNames.Plugin)]
public class CrunchyrollMovieReviewsProviderTests
{
    private readonly WireMockFixture _wireMockFixture;
    private readonly PluginConfiguration _config;
    private readonly ExtractWaybackReviewsAndCommentsTask _task;
    private readonly IWireMockAdminApi _wireMockAdminApi;
    
    public CrunchyrollMovieReviewsProviderTests(WireMockFixture wireMockFixture)
    {
        _wireMockFixture = wireMockFixture;
        _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
        _task = PluginWebApplicationFactory.Instance.Services.GetRequiredService<ExtractWaybackReviewsAndCommentsTask>();
        _wireMockAdminApi = wireMockFixture.AdminApiClient;

        _config.IsFeatureReviewsEnabled = true;
    }
    
    [Fact]
    public async Task ScrapsReviews_WhenSuccessful_GivenMovie()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        movie.PremiereDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var seriesId = movie.ProviderIds[CrunchyrollExternalKeys.SeriesId];

        PluginWebApplicationFactory.LibraryManagerMock
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns([movie]);

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        titleMetadata = titleMetadata with { CrunchyrollId = seriesId };
        await DatabaseMockHelper.CreateTitleMetadata(titleMetadata);
        
        var crunchyrollUrl = Path.Combine(
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1], 
                "series",
                seriesId,
                titleMetadata.SlugTitle)
            .Replace('\\', '/');

        var waybackMachineSearchResponse = await _wireMockAdminApi.MockWaybackMachineSearchResponse(crunchyrollUrl);
        var snapshotUrl = GetSnapshotUrl(waybackMachineSearchResponse, crunchyrollUrl);
        await _wireMockAdminApi.MockWaybackMachineArchivedUrlWithCrunchyrollReviewsHtml(snapshotUrl, _config.ArchiveOrgUrl);

        //Act
        await _task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        //Assert
        DatabaseMockHelper.ShouldHaveReviews(seriesId);
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