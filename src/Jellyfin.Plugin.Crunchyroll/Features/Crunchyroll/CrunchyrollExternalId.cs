using System.Globalization;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

public static class CrunchyrollExternalKeys
{
    public const string SeriesId = "CrunchyrollPlugin.Crunchyroll.Id";
    public const string SeasonId = "CrunchyrollPlugin.Crunchyroll.Season.Id";
    public const string EpisodeId = "CrunchyrollPlugin.Crunchyroll.Episode.Id";
}

public class CrunchyrollExternalId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.SeriesId;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
    
    public string? UrlFormatString => "https://www.crunchyroll.com/series/{0}";
    
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
        return item is Season || item is Movie;
    }
}

public class CrunchyrollExternalEpisodeId : IExternalId
{
    public string ProviderName => "Crunchyroll";

    public string Key => CrunchyrollExternalKeys.EpisodeId;
    
    public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;
    
    public string? UrlFormatString => "https://www.crunchyroll.com/watch/{0}";
    
    public bool Supports(IHasProviderIds item)
    {
        return item is Episode || item is Movie;
    }
}