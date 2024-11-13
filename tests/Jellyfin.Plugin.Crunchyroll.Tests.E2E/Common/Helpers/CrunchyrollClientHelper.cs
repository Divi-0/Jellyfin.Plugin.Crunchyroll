using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;

public static class CrunchyrollClientHelper
{
    private static readonly IServiceProvider Services =
        new ServiceCollection()
            .AddLogging()
            .AddMemoryCache()
            .AddCrunchyrollLogin()
            .AddSingleton<ICrunchyrollSessionRepository, CrunchyrollSessionRepository>()
            .AddScoped<HttpUserAgentHeaderMessageHandler>()
            .AddSingleton(new PluginConfiguration())
            .AddCrunchyrollScrapTitleMetadata()
            .BuildServiceProvider();
    
    public static async Task<ICrunchyrollSeriesClient> GetSeriesClientAsync()
    {
        var loginService = Services.GetRequiredService<ILoginService>();
        await loginService.LoginAnonymously(CancellationToken.None);
        return Services.GetRequiredService<ICrunchyrollSeriesClient>();
    }
    
    public static async Task<ICrunchyrollSeasonsClient> GetSeasonsClientAsync()
    {
        var loginService = Services.GetRequiredService<ILoginService>();
        await loginService.LoginAnonymously(CancellationToken.None);
        return Services.GetRequiredService<ICrunchyrollSeasonsClient>();
    }
    
    public static async Task<ICrunchyrollEpisodesClient> GetEpisodesClientAsync()
    {
        var loginService = Services.GetRequiredService<ILoginService>();
        await loginService.LoginAnonymously(CancellationToken.None);
        return Services.GetRequiredService<ICrunchyrollEpisodesClient>();
    }
}