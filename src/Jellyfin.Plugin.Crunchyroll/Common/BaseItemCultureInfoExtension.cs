using System;
using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public static class BaseItemCultureInfoExtension
{
    public static CultureInfo GetPreferredMetadataCultureInfo(this BaseItem item)
    {
        return BuildCultureInfo(item.GetPreferredMetadataLanguage(), item.GetPreferredMetadataCountryCode());
    }
    
    public static CultureInfo GetPreferredMetadataCultureInfo(this ItemLookupInfo info)
    {
        return BuildCultureInfo(info.MetadataLanguage, info.MetadataCountryCode);
    }

    private static CultureInfo BuildCultureInfo(string language, string countryCode)
    {
        if (language.Contains('-', StringComparison.Ordinal))
        {
            return new CultureInfo(language);
        }

        return new CultureInfo($"{language}-{countryCode}");
    }
}
