using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;

public class CrunchyrollDatabaseFixture : IAsyncLifetime
{
    public string DbFilePath { get; private set; } = string.Empty;
    
    public Task InitializeAsync()
    {
        var location = typeof(CrunchyrollUnitOfWork).Assembly.Location;
        DbFilePath = Path.Combine(Path.GetDirectoryName(location)!, $"Crunchyroll_{Guid.NewGuid()}.db");

        _ = Task.Run(() =>
        {
            while (CrunchyrollPlugin.Instance is null)
            {
            }

            var config = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
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