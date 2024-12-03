using System;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;

public static class DeleteTitleMetadataServiceExtension
{
    public static void AddDeleteTitleMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IDeleteTitleMetadataRepository, DeleteTitleMetadataRepository>();
        serviceCollection.AddScoped<IDeleteTitleMetadataService, DeleteTitleMetadataService>();
    }
    
    public static void UseDeleteTitleMetadata(this IServiceProvider serviceProvider)
    {
        var libraryManager = serviceProvider.GetRequiredService<ILibraryManager>();
        libraryManager.ItemRemoved += async (_, args) =>
        {
            var scope = serviceProvider.CreateScope();
            var deleteTitleMetadataService = scope.ServiceProvider.GetRequiredService<IDeleteTitleMetadataService>();
            await deleteTitleMetadataService.DeleteTitleMetadataAsync(args.Item);
        };
    }
}