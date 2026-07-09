using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.FlareSolverr;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.FlareSolverrTest;

public static class ServiceExtension
{
    public static IServiceCollection AddFlareSolverrTest(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<ICrunchyrollClient, CrunchyrollClient>()
            .AddHttpMessageHandler<FlareSolverrMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddSingleton<ILoginService, LoginService>();
        
        return serviceCollection;
    }
}