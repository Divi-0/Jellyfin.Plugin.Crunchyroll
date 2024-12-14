using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;

public class PluginWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    public static PluginWebApplicationFactory Instance { get; private set; } = null!;
    public static MockHttpMessageHandler CrunchyrollHttpMessageHandlerMock { get; private set; } = null!;
    public static ILibraryManager LibraryManagerMock { get; private set; } = null!;
    public static IItemRepository ItemRepositoryMock { get; private set; } = null!;
    public static IMediaSourceManager MediaSourceManagerMock { get; private set; } = null!;

    public PluginWebApplicationFactory()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        CrunchyrollHttpMessageHandlerMock = mockHttpMessageHandler;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSingleton<IApplicationPaths>(Substitute.For<IApplicationPaths>());
            LibraryManagerMock = Substitute.For<ILibraryManager>();
            ItemRepositoryMock = Substitute.For<IItemRepository>();
            MediaSourceManagerMock = Substitute.For<IMediaSourceManager>();
            BaseItem.LibraryManager = LibraryManagerMock;
            BaseItem.ItemRepository = ItemRepositoryMock;
            BaseItem.MediaSourceManager = MediaSourceManagerMock;
            services.AddSingleton<ILibraryManager>(LibraryManagerMock);
            services.AddSingleton<IItemRepository>(ItemRepositoryMock);
            services.AddSingleton<IMediaSourceManager>(MediaSourceManagerMock);

            var xmlSerializer = Substitute.For<IXmlSerializer>();
            xmlSerializer.DeserializeFromFile(Arg.Is(typeof(PluginConfiguration)), Arg.Any<string>())
                .Returns(new PluginConfiguration());
            services.AddSingleton<IXmlSerializer>(xmlSerializer);

            services.AddSingleton<CrunchyrollSeriesProvider>();
            services.AddSingleton<CrunchyrollSeasonProvider>();
            services.AddSingleton<CrunchyrollEpisodeProvider>();
            services.AddSingleton<CrunchyrollMovieProvider>();
            
            services.AddSingleton<CrunchyrollSeriesReviewsProvider>();
            services.AddSingleton<CrunchyrollEpisodeCommentsProvider>();
            services.AddSingleton<CrunchyrollMovieReviewsProvider>();
            services.AddSingleton<CrunchyrollMovieCommentsProvider>();
            
            services.AddSingleton<CrunchyrollSeriesImageProvider>();
            services.AddSingleton<CrunchyrollEpisodeImageProvider>();
            services.AddSingleton<CrunchyrollMovieImageProvider>();
        });
        
        builder.UseEnvironment("Development");
    }

    public static void CreateInstance()
    {
        Instance = new PluginWebApplicationFactory();
    }
}