using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.WaybackMachine;

public class ExtractWaybackReviewsAndCommentsTaskTests
{
    [Fact]
    public async Task ExecuteAsync_DoesNothing_WhenFeaturesAreDisabled()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var config = new PluginConfiguration { IsFeatureReviewsEnabled = false, IsFeatureCommentsEnabled = false };
        var sp = new ServiceCollection().AddSingleton(config).BuildServiceProvider();
        var sut = new ExtractWaybackReviewsAndCommentsTask(libraryManager, sp, Substitute.For<ILogger<ExtractWaybackReviewsAndCommentsTask>>());

        await sut.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        libraryManager.DidNotReceive().GetItemList(Arg.Any<InternalItemsQuery>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipsPostCutoffAndDeduplicatesEligibleItems()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var config = new PluginConfiguration { IsFeatureReviewsEnabled = true, IsFeatureCommentsEnabled = true };
        var seriesReviews = Substitute.For<ICrunchyrollSeriesReviewsService>();
        var movieReviews = Substitute.For<ICrunchyrollMovieReviewsService>();
        var episodeComments = Substitute.For<ICrunchyrollEpisodeCommentsService>();
        var movieComments = Substitute.For<ICrunchyrollMovieCommentsService>();

        var sp = new ServiceCollection()
            .AddSingleton(config)
            .AddScoped(_ => seriesReviews)
            .AddScoped(_ => movieReviews)
            .AddScoped(_ => episodeComments)
            .AddScoped(_ => movieComments)
            .BuildServiceProvider();

        var oldSeries1 = new Series
        {
            Id = Guid.NewGuid(),
            PreferredMetadataLanguage = "pt-BR",
            PreferredMetadataCountryCode = "BR",
            PremiereDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ProviderIds = new Dictionary<string, string> { [CrunchyrollExternalKeys.SeriesId] = "S1" }
        };
        var duplicateSeries = new Series
        {
            Id = Guid.NewGuid(),
            PreferredMetadataLanguage = "pt-BR",
            PreferredMetadataCountryCode = "BR",
            PremiereDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ProviderIds = new Dictionary<string, string> { [CrunchyrollExternalKeys.SeriesId] = "S1" }
        };
        var newSeries = new Series
        {
            Id = Guid.NewGuid(),
            PreferredMetadataLanguage = "pt-BR",
            PreferredMetadataCountryCode = "BR",
            PremiereDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ProviderIds = new Dictionary<string, string> { [CrunchyrollExternalKeys.SeriesId] = "S2" }
        };
        var oldEpisode = new Episode
        {
            Id = Guid.NewGuid(),
            PreferredMetadataLanguage = "pt-BR",
            PreferredMetadataCountryCode = "BR",
            PremiereDate = new DateTime(2023, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            ProviderIds = new Dictionary<string, string> { [CrunchyrollExternalKeys.EpisodeId] = "E1" }
        };
        var oldMovie = new Movie
        {
            Id = Guid.NewGuid(),
            PreferredMetadataLanguage = "pt-BR",
            PreferredMetadataCountryCode = "BR",
            PremiereDate = null,
            ProviderIds = new Dictionary<string, string>
            {
                [CrunchyrollExternalKeys.SeriesId] = "MS1",
                [CrunchyrollExternalKeys.EpisodeId] = "ME1"
            }
        };

        libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns([oldSeries1, duplicateSeries, newSeries, oldEpisode, oldMovie]);

        var sut = new ExtractWaybackReviewsAndCommentsTask(libraryManager, sp, Substitute.For<ILogger<ExtractWaybackReviewsAndCommentsTask>>());
        await sut.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        await seriesReviews.Received(1).ScrapReviewsAsync(oldSeries1, Arg.Any<CancellationToken>());
        await seriesReviews.DidNotReceive().ScrapReviewsAsync(duplicateSeries, Arg.Any<CancellationToken>());
        await seriesReviews.DidNotReceive().ScrapReviewsAsync(newSeries, Arg.Any<CancellationToken>());
        await episodeComments.Received(1).ScrapCommentsAsync(oldEpisode, Arg.Any<CancellationToken>());
        await movieReviews.Received(1).ScrapReviewsAsync(oldMovie, Arg.Any<CancellationToken>());
        await movieComments.Received(1).ScrapCommentsAsync(oldMovie, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_StopsImmediately_WhenCancelled()
    {
        var libraryManager = Substitute.For<ILibraryManager>();
        var config = new PluginConfiguration { IsFeatureReviewsEnabled = true };
        var seriesReviews = Substitute.For<ICrunchyrollSeriesReviewsService>();
        using var cts = new CancellationTokenSource();

        seriesReviews.ScrapReviewsAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromResult(ItemUpdateType.None);
            });

        var sp = new ServiceCollection()
            .AddSingleton(config)
            .AddScoped(_ => seriesReviews)
            .BuildServiceProvider();

        var s1 = new Series { Id = Guid.NewGuid(), PreferredMetadataLanguage = "pt-BR", PreferredMetadataCountryCode = "BR", ProviderIds = new Dictionary<string, string> { [CrunchyrollExternalKeys.SeriesId] = "S1" } };
        var s2 = new Series { Id = Guid.NewGuid(), PreferredMetadataLanguage = "pt-BR", PreferredMetadataCountryCode = "BR", ProviderIds = new Dictionary<string, string> { [CrunchyrollExternalKeys.SeriesId] = "S2" } };
        libraryManager.GetItemList(Arg.Any<InternalItemsQuery>()).Returns([s1, s2]);

        var sut = new ExtractWaybackReviewsAndCommentsTask(libraryManager, sp, Substitute.For<ILogger<ExtractWaybackReviewsAndCommentsTask>>());
        var act = async () => await sut.ExecuteAsync(new Progress<double>(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await seriesReviews.Received(1).ScrapReviewsAsync(s1, Arg.Any<CancellationToken>());
        await seriesReviews.DidNotReceive().ScrapReviewsAsync(s2, Arg.Any<CancellationToken>());
    }
}
