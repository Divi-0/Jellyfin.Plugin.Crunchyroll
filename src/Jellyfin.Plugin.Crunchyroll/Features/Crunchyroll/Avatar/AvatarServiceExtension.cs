using System;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;

public static class AvatarServiceExtension
{
    public static IServiceCollection AddCrunchyrollAvatar(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGetAvatarRepository, AvatarRepository>();
        serviceCollection.AddSingleton<IAddAvatarRepository, AvatarRepository>();
        serviceCollection.AddSingleton<IAddAvatarService, AddAvatarService>();
        
        serviceCollection.AddHttpClient<IAvatarClient, AvatarClient>(httpclient => 
            {
                httpclient.Timeout = TimeSpan.FromSeconds(180);
            })
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}