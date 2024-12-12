using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;

public static class GetSpecialEpisodeCrunchyrollIdServiceExtension
{
    public static void AddGetSpecialEpisodeCrunchyrollId(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddScoped<IGetSpecialEpisodeCrunchyrollIdRepository, GetSpecialEpisodeCrunchyrollIdRepository>();
        serviceCollection
            .AddScoped<IGetSpecialEpisodeCrunchyrollIdService, GetSpecialEpisodeCrunchyrollIdService>();
    }
}