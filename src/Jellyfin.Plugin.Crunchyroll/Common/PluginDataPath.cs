using System.IO;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Crunchyroll.Common;

/// <summary>
/// Location of the plugin's persistent data (database, avatar images).
/// Jellyfin installs every plugin version into its own folder and deletes the old folder on the next
/// startup, so nothing that has to survive an update may be stored next to the plugin assembly.
/// </summary>
public static class PluginDataPath
{
    private const string DirectoryName = "Jellyfin.Plugin.Crunchyroll";

    /// <summary>
    /// Defaults to the assembly folder until <see cref="Initialize"/> is called, e.g. for the EF design time tooling.
    /// </summary>
    public static string Directory { get; private set; } =
        Path.GetDirectoryName(typeof(PluginDataPath).Assembly.Location)!;

    public static string DatabaseFile => Path.Combine(Directory, "Crunchyroll.db");

    public static string AvatarImagesDirectory => Path.Combine(Directory, "avatar-images");

    public static void Initialize(IApplicationPaths applicationPaths)
    {
        Directory = Path.Combine(applicationPaths.PluginConfigurationsPath, DirectoryName);
        System.IO.Directory.CreateDirectory(Directory);
    }
}