using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

public static class CrunchyrollExternalKeys
{
    public const string Id = "CrunchyrollPlugin.Crunchyroll.Id";
    public const string SlugTitle = "CrunchyrollPlugin.Crunchyroll.SlugTitle";
    public const string SeasonId = "CrunchyrollPlugin.Crunchyroll.Season.Id";
    public const string EpisodeId = "CrunchyrollPlugin.Crunchyroll.Episode.Id";
    public const string EpisodeSlugTitle = "CrunchyrollPlugin.Crunchyroll.Episode.SlugTitle";
}

public class CrunchyrollExternalId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.Id;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
    
    public string? UrlFormatString => $"https://www.crunchyroll.com/{CrunchyrollPlugin.Instance!.Configuration.CrunchyrollLanguage}/series/{0}";
    
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
    
    public string? UrlFormatString => string.Empty;
    
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
    
    public string? UrlFormatString => string.Empty;
    
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
    
    public string? UrlFormatString => string.Empty;
    
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
    
    public string? UrlFormatString => string.Empty;
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Episode;
    }
}