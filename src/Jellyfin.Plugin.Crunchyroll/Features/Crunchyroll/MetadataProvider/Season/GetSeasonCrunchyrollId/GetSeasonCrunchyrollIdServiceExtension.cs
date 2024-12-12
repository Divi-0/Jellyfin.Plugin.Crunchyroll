using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;

public static class GetSeasonCrunchyrollIdServiceExtension
{
    public static void AddGetSeasonCrunchyrollId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetSeasonCrunchyrollIdService, GetSeasonCrunchyrollIdService>();
        serviceCollection.AddScoped<IGetSeasonCrunchyrollIdRepository, GetSeasonCrunchyrollIdRepository>();
    }
}