using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments;

public static class CommentsServiceExtensions
{
    public static IServiceCollection AddCrunchyrollComments(this IServiceCollection services, PluginConfiguration configuration)
    {
        services.AddExtractComments();
        services.AddGetComments(configuration);

        return services;
    }
}