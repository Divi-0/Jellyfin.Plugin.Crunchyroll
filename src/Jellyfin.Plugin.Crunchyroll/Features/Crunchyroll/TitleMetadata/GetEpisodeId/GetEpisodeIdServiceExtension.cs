using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public static class GetEpisodeIdServiceExtension
{
    public static IServiceCollection AddCrunchyrollGetEpisodeId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetEpisodeRepository, GetEpisodeRepository>();
        
        return serviceCollection;
    }
}