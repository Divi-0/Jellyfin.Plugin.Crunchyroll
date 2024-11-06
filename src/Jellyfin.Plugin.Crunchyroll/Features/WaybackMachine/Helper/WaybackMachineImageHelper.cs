namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Helper;

public static class WaybackMachineImageHelper
{
    /// <param name="waybackMachineImageUri"></param>
    /// <returns>Returns the archived uri without web.archive.org url;
    /// if web.archive.org is not present it returns the param <see cref="waybackMachineImageUri"/></returns>
    public static string GetArchivedImageUri(string waybackMachineImageUri)
    {
        string? crunchyrollUri = null;

        if (waybackMachineImageUri.Contains("web.archive.org"))
        {
            crunchyrollUri = waybackMachineImageUri.Split("im_/")[1];
        }

        return string.IsNullOrWhiteSpace(crunchyrollUri) 
            ? waybackMachineImageUri 
            : crunchyrollUri;
    }
}