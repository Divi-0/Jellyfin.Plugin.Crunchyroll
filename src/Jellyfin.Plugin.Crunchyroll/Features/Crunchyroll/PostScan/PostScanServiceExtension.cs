using System.IO.Abstractions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinDataTask;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

internal static class PostScanServiceExtension
{
    public static IServiceCollection AddCrunchyrollPostScanTasks(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IFileSystem, FileSystem>();
        serviceCollection.AddScoped<IFile>(serviceProvider => serviceProvider.GetRequiredService<IFileSystem>().File);
        serviceCollection.AddScoped<IDirectory>(serviceProvider => serviceProvider.GetRequiredService<IFileSystem>().Directory);
        
        serviceCollection.AddSingleton<IPostScanTask, SetTitleIdTask>();
            
        serviceCollection.AddSingleton<IPostTitleIdSetTask, ScrapTitleMetadataTask>();
        serviceCollection.AddSingleton<IPostTitleIdSetTask, SetSeasonIdTask>();
        serviceCollection.AddSingleton<IPostTitleIdSetTask, ExtractReviewsTask>();
        serviceCollection.AddScoped<IPostTitleIdSetTask, SetSeriesImagesTask>();
            
        serviceCollection.AddSingleton<IPostSeasonIdSetTask, SetEpisodeIdTask>();
        
        serviceCollection.AddSingleton<IPostEpisodeIdSetTask, ExtractCommentsTask>();

        serviceCollection.AddOverwriteEpisodeJellyfinDataTask();

        return serviceCollection;
    }
}