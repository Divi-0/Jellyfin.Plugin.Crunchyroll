using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public static class BaseItemCultureInfoExtension
{
    public static CultureInfo GetPreferredMetadataCultureInfo(this BaseItem item)
    {
        return new CultureInfo($"{item.GetPreferredMetadataLanguage()}-{item.GetPreferredMetadataCountryCode()}");
    }
    
    public static CultureInfo GetPreferredMetadataCultureInfo(this ItemLookupInfo info)
    {
        return new CultureInfo($"{info.MetadataLanguage}-{info.MetadataCountryCode}");
    }
}