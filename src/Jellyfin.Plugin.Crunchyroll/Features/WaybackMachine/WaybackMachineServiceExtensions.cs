using System;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;

public static class WaybackMachineServiceExtensions
{
    public static IServiceCollection AddWaybackMachine(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<IWaybackMachineClient, WaybackMachineClient>(httpclient =>
            {
                httpclient.Timeout = TimeSpan.FromSeconds(180);
            })
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}