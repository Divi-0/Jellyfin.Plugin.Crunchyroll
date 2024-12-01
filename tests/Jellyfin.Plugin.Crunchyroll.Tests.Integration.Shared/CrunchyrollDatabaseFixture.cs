using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;

public class CrunchyrollDatabaseFixture : IAsyncLifetime
{
    public string DbFilePath { get; private set; } = string.Empty;
    
    public Task InitializeAsync()
    {
        var location = typeof(CrunchyrollDbContext).Assembly.Location;
        DbFilePath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");

        _ = Task.Run(() =>
        {
            while (CrunchyrollPlugin.Instance is null)
            {
            }

            var config = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
            config.LocalDatabasePath = Path.GetDirectoryName(location)!;
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