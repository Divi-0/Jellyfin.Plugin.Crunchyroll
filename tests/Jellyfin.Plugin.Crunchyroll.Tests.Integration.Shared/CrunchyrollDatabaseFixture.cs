using Jellyfin.Plugin.Crunchyroll.Common;
using Xunit;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;

public class CrunchyrollDatabaseFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try
        {
            File.Delete(PluginDataPath.DatabaseFile);
        }
        catch
        {
            //ignore
        }

        return Task.CompletedTask;
    }
}