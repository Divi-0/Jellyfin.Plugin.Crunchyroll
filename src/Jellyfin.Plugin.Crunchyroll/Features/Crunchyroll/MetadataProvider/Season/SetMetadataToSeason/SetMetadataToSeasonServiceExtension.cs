using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;

public static class SetMetadataToSeasonServiceExtension
{
    public static void AddSetMetadataToSeason(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISetMetadataToSeasonService, SetMetadataToSeasonService>();
        serviceCollection.AddScoped<ISetMetadataToSeasonRepository, SetMetadataToSeasonRepository>();
    }
}