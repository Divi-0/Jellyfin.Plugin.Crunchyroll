using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments;

public static class CommentsServiceExtensions
{
    public static IServiceCollection AddCrunchyrollComments(this IServiceCollection services)
    {
        services.AddExtractComments();
        services.AddGetComments();

        return services;
    }
}