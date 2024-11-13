using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;

public static class LoginServiceExtension
{
    public static IServiceCollection AddCrunchyrollLogin(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<ICrunchyrollLoginClient, CrunchyrollLoginClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddSingleton<ILoginService, LoginService>();
        
        return serviceCollection;
    }
}