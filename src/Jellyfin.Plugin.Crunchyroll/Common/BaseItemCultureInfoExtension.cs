using System.Globalization;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public static class BaseItemCultureInfoExtension
{
    public static CultureInfo GetPreferredMetadataCultureInfo(this BaseItem item)
    {
        return new CultureInfo($"{item.GetPreferredMetadataLanguage()}-{item.GetPreferredMetadataCountryCode()}");
    }
}