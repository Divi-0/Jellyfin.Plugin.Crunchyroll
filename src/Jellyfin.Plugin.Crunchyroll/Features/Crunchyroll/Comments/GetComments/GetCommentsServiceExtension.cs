using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;

public static class GetCommentsServiceExtension
{
    public static IServiceCollection AddGetComments(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<ICrunchyrollGetCommentsClient, CrunchyrollGetCommentsClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddSingleton<IGetCommentsSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}