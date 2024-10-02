using Jellyfin.Plugin.ExternalComments.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.ExtractComments;

public static class ExtractCommentsServiceExtensions
{
    public static IServiceCollection AddExtractComments(this IServiceCollection services)
    {
        services.AddHttpClient<IHtmlCommentsExtractor, HtmlCommentsExtractor>()
            .AddPollyHttpClientDefaultPolicy();

        services.AddSingleton<IExtractCommentsSession, CrunchyrollUnitOfWork>();

        return services;
    }
}