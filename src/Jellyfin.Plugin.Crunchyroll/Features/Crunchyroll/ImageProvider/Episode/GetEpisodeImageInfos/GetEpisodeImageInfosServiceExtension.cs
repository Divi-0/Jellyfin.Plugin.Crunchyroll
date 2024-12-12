using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;

public static class GetEpisodeImageInfosServiceExtension
{
    public static void AddGetEpisodeImageInfos(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetEpisodeImageInfosRepository, GetEpisodeImageInfosRepository>();
        serviceCollection.AddScoped<IGetEpisodeImageInfosService, GetEpisodeImageInfosService>();
    }
}