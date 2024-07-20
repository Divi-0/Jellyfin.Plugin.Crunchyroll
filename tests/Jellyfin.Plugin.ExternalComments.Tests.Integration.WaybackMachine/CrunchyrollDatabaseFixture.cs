using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.WaybackMachine;

public class CrunchyrollDatabaseFixture : IAsyncLifetime
{
    public string DbFilePath { get; private set; } = string.Empty;
    
    public Task InitializeAsync()
    {
        var location = typeof(ReviewsUnitOfWork).Assembly.Location;
        DbFilePath = Path.Combine(Path.GetDirectoryName(location)!, $"Crunchyroll_{Guid.NewGuid()}.db");

        _ = Task.Run(() =>
        {
            while (ExternalCommentsPlugin.Instance is null)
            {
            }

            var config = ExternalCommentsPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
            config.LocalDatabasePath = DbFilePath;
        });

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try
        {
            File.Delete(DbFilePath);
        }
        catch
        {
            //ignore
        }
        
        return Task.CompletedTask;
    }
}