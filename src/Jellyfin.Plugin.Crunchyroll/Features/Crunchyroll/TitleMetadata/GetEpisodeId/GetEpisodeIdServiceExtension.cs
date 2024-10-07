using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public static class GetEpisodeIdServiceExtension
{
    public static IServiceCollection AddCrunchyrollGetEpisodeId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGetEpisodeSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}