using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

public class CrunchyrollExternalUrlProvider : IExternalUrlProvider
{
    public string Name => "Crunchyroll";
    
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Series series:
                var seriesId = series.GetProviderId(CrunchyrollExternalKeys.SeriesId);
                
                return !string.IsNullOrWhiteSpace(seriesId) 
                    ? [$"https://www.crunchyroll.com/series/{seriesId}"] 
                    : [];
            case Episode episode:
                var episodeId = episode.GetProviderId(CrunchyrollExternalKeys.EpisodeId);
                
                return !string.IsNullOrWhiteSpace(episodeId) 
                    ? [$"https://www.crunchyroll.com/watch/{episodeId}"] 
                    : [];
            default:
                return [];
        }
    }
}