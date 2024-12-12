using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;

public static class SeasonMetadataProviderServiceExtension
{
    public static void AddSeasonMetadataProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetSeasonCrunchyrollId();
        serviceCollection.AddScrapSeasonMetadata();
        serviceCollection.AddSetMetadataToSeason();
        serviceCollection.AddScoped<ISeasonGetMetadataService, SeasonGetMetadataService>();
    }
}