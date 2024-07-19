using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine;

public static class WaybackMachineServiceExtensions
{
    public static IServiceCollection AddWaybackMachine(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<IWaybackMachineClient, WaybackMachineClient>()
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}