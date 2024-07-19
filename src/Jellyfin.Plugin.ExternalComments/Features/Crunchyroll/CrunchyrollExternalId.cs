using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public static class CrunchyrollExternalKeys
{
    public const string Id = "ExternalCommentsPlugin.Crunchyroll.Id";
    public const string SlugTitle = "ExternalCommentsPlugin.Crunchyroll.SlugTitle";
}

public class CrunchyrollExternalId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.Id;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
    
    public string? UrlFormatString => $"https://www.crunchyroll.com/{ExternalCommentsPlugin.Instance!.Configuration.CrunchyrollLanguage}/series/{0}";
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Series;
    }
}

public class CrunchyrollExternalSlugTitle : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.SlugTitle;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
    
    public string? UrlFormatString => null;
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Series;
    }
}