using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine;

public static class WaybackMachineServiceExtensions
{
    public static IServiceCollection AddWaybackMachine(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<IWaybackMachineClient, WaybackMachineClient>()
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}