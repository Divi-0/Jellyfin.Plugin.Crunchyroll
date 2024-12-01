using System;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public static class ExtractCommentsServiceExtensions
{
    public static IServiceCollection AddExtractComments(this IServiceCollection services)
    {
        services.AddHttpClient<IHtmlCommentsExtractor, HtmlCommentsExtractor>(httpclient =>
            {
                httpclient.Timeout = TimeSpan.FromSeconds(180);
            })
            .AddPollyHttpClientDefaultPolicy();

        services.AddScoped<IExtractCommentsRepository, ExtractCommentsRepository>();

        return services;
    }
}