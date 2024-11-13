using System;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;

public static class AvatarServiceExtension
{
    public static IServiceCollection AddCrunchyrollAvatar(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGetAvatarSession, CrunchyrollUnitOfWork>();
        serviceCollection.AddSingleton<IAddAvatarSession, CrunchyrollUnitOfWork>();
        serviceCollection.AddSingleton<IAddAvatarService, AddAvatarService>();
        
        serviceCollection.AddHttpClient<IAvatarClient, AvatarClient>(httpclient => 
            {
                httpclient.Timeout = TimeSpan.FromSeconds(180);
            })
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}