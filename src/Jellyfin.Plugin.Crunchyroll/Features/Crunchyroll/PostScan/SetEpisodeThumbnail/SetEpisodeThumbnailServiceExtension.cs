using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;

public static class SetEpisodeThumbnailServiceExtension
{
    public static void AddSetEpisodeThumbnail(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ISetEpisodeThumbnail, SetEpisodeThumbnail>();
    }
}