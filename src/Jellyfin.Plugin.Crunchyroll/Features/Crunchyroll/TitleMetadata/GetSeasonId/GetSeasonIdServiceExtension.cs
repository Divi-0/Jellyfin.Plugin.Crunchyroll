using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public static class GetSeasonIdServiceExtension
{
    public static IServiceCollection AddCrunchyrollGetSeasonId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGetSeasonSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}