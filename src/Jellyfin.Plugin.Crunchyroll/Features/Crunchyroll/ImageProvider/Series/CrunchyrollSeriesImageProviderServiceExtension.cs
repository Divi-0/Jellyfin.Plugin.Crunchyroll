using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series;

public static class CrunchyrollSeriesImageProviderServiceExtension
{
    public static void AddCrunchyrollSeriesImageProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetSeriesImageInfos();
    }
}