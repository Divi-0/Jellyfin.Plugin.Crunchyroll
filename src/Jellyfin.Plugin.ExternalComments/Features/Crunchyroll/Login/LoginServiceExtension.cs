using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login.Client;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;

public static class LoginServiceExtension
{
    public static IServiceCollection AddCrunchyrollLogin(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollLoginClient, CrunchyrollLoginClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoginPipelineBehavior<,>));
        
        return serviceCollection;
    }
}