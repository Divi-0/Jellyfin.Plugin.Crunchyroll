using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;

public static class AvatarServiceExtension
{
    public static IServiceCollection AddCrunchyrollAvatar(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddSingleton<IGetAvatarSession, CrunchyrollUnitOfWork>();
        serviceCollection.AddSingleton<IAddAvatarSession, CrunchyrollUnitOfWork>();
        
        serviceCollection.AddHttpClient<IAvatarClient, AvatarClient>()
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}