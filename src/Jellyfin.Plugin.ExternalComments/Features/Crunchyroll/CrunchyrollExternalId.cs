using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public static class CrunchyrollExternalKeys
{
    public const string Id = "ExternalCommentsPlugin.Crunchyroll.Id";
    public const string SlugTitle = "ExternalCommentsPlugin.Crunchyroll.SlugTitle";
    public const string SeasonId = "ExternalCommentsPlugin.Crunchyroll.Season.Id";
    public const string EpisodeId = "ExternalCommentsPlugin.Crunchyroll.Episode.Id";
    public const string EpisodeSlugTitle = "ExternalCommentsPlugin.Crunchyroll.Episode.SlugTitle";
}

public class CrunchyrollExternalId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.Id;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
    
    public string? UrlFormatString => $"https://www.crunchyroll.com/{ExternalCommentsPlugin.Instance!.Configuration.CrunchyrollLanguage}/series/{0}";
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Series || item is Movie;
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
        return item is Series || item is Movie;
    }
}

public class CrunchyrollExternalSeasonId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.SeasonId;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
    
    public string? UrlFormatString => null;
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Season;
    }
}

public class CrunchyrollExternalEpisodeId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.EpisodeId;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;
    
    public string? UrlFormatString => null;
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Episode;
    }
}

public class CrunchyrollExternalEpisodeSlugTitle : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.EpisodeSlugTitle;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;
    
    public string? UrlFormatString => null;
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Episode;
    }
}