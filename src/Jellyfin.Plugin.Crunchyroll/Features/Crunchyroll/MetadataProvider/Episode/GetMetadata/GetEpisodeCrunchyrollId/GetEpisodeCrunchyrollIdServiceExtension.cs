using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;

public static class GetEpisodeCrunchyrollIdServiceExtension
{
    public static void AddGetEpisodeCrunchyrollId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetEpisodeCrunchyrollIdRepository, GetEpisodeCrunchyrollIdRepository>();
        serviceCollection.AddScoped<IGetEpisodeCrunchyrollIdService, GetEpisodeCrunchyrollIdService>();
    }
}