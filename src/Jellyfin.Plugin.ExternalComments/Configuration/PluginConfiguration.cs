using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ExternalComments.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string FlareSolverrUrl { get; set; } = string.Empty;
    public int FlareSolverrTimeout { get; set; } = 15000;
    public string FlareSolverrProxyUrl { get; set; } = string.Empty;
    public string FlareSolverrProxyUsername { get; set; } = string.Empty;
    public string FlareSolverrProxyPassword { get; set; } = string.Empty;
    public string CrunchyrollUrl { get; set; } = "https://www.crunchyroll.com/";
    public string CrunchyrollLanguage { get; set; } = "de-DE";
    public string ArchiveOrgUrl { get; set; } = "http://web.archive.org";
    public string LocalDatabasePath { get; set; } = string.Empty;
    public bool IsWaybackMachineEnabled { get; set; } = true;
}