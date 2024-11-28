using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Crunchyroll.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string CrunchyrollUrl { get; set; } = "https://www.crunchyroll.com/";
    public string ArchiveOrgUrl { get; set; } = "http://web.archive.org";
    public string LocalDatabasePath { get; set; } = string.Empty;
    public bool IsWaybackMachineEnabled { get; set; } = true;
    public string LibraryPath { get; set; } = string.Empty;
    public int WaybackMachineWaitTimeoutInSeconds { get; set; } = 180;
    public bool IsOrderSeasonsByCrunchyrollOrderEnabled { get; set; } = true;
}