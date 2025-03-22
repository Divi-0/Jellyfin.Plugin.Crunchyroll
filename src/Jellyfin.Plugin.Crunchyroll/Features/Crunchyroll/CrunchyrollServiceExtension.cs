using System;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

public static class CrunchyrollServiceExtension
{
    public static IServiceCollection AddCrunchyroll(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ICrunchyrollSessionRepository, CrunchyrollSessionRepository>();
        serviceCollection.AddSingleton<TimeProvider>(TimeProvider.System);
        serviceCollection.AddTransient<HttpUserAgentHeaderMessageHandler>();
        
        serviceCollection.AddCrunchyrollLogin();
        serviceCollection.AddCrunchyrollComments();
        serviceCollection.AddCrunchyrollGetReviews();
        serviceCollection.AddCrunchyrollExtractReviews();
        serviceCollection.AddCrunchyrollAvatar();
        
        serviceCollection.AddDeleteTitleMetadata();
        
        serviceCollection.AddMetadataProvider();
        serviceCollection.AddImageProvider();
        
        return serviceCollection;
    }
}