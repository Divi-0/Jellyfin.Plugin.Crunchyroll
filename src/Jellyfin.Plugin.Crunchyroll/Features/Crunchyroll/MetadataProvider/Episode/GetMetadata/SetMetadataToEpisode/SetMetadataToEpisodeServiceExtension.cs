using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;

public static class SetMetadataToEpisodeServiceExtension
{
    public static void AddSetMetadataToEpisode(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISetMetadataToEpisodeRepository, SetMetadataToEpisodeRepository>();
        serviceCollection.AddScoped<ISetMetadataToEpisodeService, SetMetadataToEpisodeService>();
    }
}