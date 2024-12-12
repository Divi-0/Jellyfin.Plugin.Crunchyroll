using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;

public static class GetSeriesImageInfosServiceExtension
{
    public static void AddGetSeriesImageInfos(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetSeriesImageInfosRepository, GetSeriesImageInfosRepository>();
        serviceCollection.AddScoped<IGetSeriesImageInfosService, GetSeriesImageInfosService>();
    }
}