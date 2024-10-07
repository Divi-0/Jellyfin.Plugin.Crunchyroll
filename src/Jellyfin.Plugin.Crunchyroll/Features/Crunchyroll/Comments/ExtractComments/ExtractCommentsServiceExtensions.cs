using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

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