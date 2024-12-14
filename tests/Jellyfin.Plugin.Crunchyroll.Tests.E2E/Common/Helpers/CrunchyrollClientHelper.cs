using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;

public static class CrunchyrollClientHelper
{
    private static IServiceProvider? _services = null!;
    private static IServiceProvider Services
    {
        get
        {
            if (_services != null)
            {
                return _services;
            }
            
            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddMemoryCache()
                .AddCrunchyrollLogin()
                .AddSingleton<ICrunchyrollSessionRepository, CrunchyrollSessionRepository>()
                .AddScoped<HttpUserAgentHeaderMessageHandler>()
                .AddSingleton(new PluginConfiguration());

            serviceCollection.AddScrapEpisodeMetadata();
            serviceCollection.AddScrapSeasonMetadata();
            serviceCollection.AddScrapSeriesMetadata();

            _services = serviceCollection.BuildServiceProvider();
            return _services;
        }
    }

    public static async Task<ICrunchyrollSeriesClient> GetSeriesClientAsync()
    {
        var loginService = Services.GetRequiredService<ILoginService>();
        await loginService.LoginAnonymouslyAsync(CancellationToken.None);
        return Services.GetRequiredService<ICrunchyrollSeriesClient>();
    }
    
    public static async Task<ICrunchyrollSeasonsClient> GetSeasonsClientAsync()
    {
        var loginService = Services.GetRequiredService<ILoginService>();
        await loginService.LoginAnonymouslyAsync(CancellationToken.None);
        return Services.GetRequiredService<ICrunchyrollSeasonsClient>();
    }
    
    public static async Task<IScrapEpisodeCrunchyrollClient> GetEpisodesClientAsync()
    {
        var loginService = Services.GetRequiredService<ILoginService>();
        await loginService.LoginAnonymouslyAsync(CancellationToken.None);
        return Services.GetRequiredService<IScrapEpisodeCrunchyrollClient>();
    }
}