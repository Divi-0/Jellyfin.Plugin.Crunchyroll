using System.IO.Abstractions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeriesJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;

internal static class PostScanServiceExtension
{
    public static IServiceCollection AddCrunchyrollPostScanTasks(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IFileSystem, FileSystem>();
        serviceCollection.AddScoped<IFile>(serviceProvider => serviceProvider.GetRequiredService<IFileSystem>().File);
        serviceCollection.AddScoped<IDirectory>(serviceProvider => serviceProvider.GetRequiredService<IFileSystem>().Directory);
        
        serviceCollection.AddSingleton<IPostSeriesScanTask, SetTitleIdTask>();
            
        serviceCollection.AddSingleton<IPostTitleIdSetTask, ScrapTitleMetadataTask>();
        serviceCollection.AddSingleton<IPostTitleIdSetTask, SetSeasonIdTask>();
        serviceCollection.AddSingleton<IPostTitleIdSetTask, ExtractReviewsTask>();
            
        serviceCollection.AddOverwriteSeasonJellyfinData();
        serviceCollection.AddSingleton<IPostSeasonIdSetTask, SetEpisodeIdTask>();
        
        serviceCollection.AddSingleton<IPostEpisodeIdSetTask, ExtractCommentsTask>();

        serviceCollection.AddOverwriteSeriesJellyfinData();
        serviceCollection.AddOverwriteEpisodeJellyfinData();
        serviceCollection.AddSetMovieEpisodeId();
        
        serviceCollection.AddSingleton<IPostMovieIdSetTask, ScrapTitleMetadataTask>();
        serviceCollection.AddOverwriteMovieJellyfinData();

        return serviceCollection;
    }
}