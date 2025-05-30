using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Crunchyroll.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string CrunchyrollUrl { get; set; } = "https://www.crunchyroll.com/";
    public string ArchiveOrgUrl { get; set; } = "http://web.archive.org";
    public string LocalDatabasePath { get; set; } = string.Empty;
    public int WaybackMachineWaitTimeoutInSeconds { get; set; } = 120;
    
    //Series Features
    public bool IsFeatureSeriesTitleEnabled { get; set; } = false;
    public bool IsFeatureSeriesDescriptionEnabled { get; set; } = false;
    public bool IsFeatureSeriesStudioEnabled { get; set; } = false;
    public bool IsFeatureSeriesRatingsEnabled { get; set; } = false;
    
    //Season Features
    public bool IsFeatureSeasonTitleEnabled { get; set; } = false;
    public bool IsFeatureSeasonOrderByCrunchyrollOrderEnabled { get; set; } = false;
    
    //Episode Features
    public bool IsFeatureEpisodeTitleEnabled { get; set; } = false;
    public bool IsFeatureEpisodeDescriptionEnabled { get; set; } = false;
    public bool IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled { get; set; } = false;
    
    //Movie Features
    public bool IsFeatureMovieTitleEnabled { get; set; } = false;
    public bool IsFeatureMovieDescriptionEnabled { get; set; } = false;
    public bool IsFeatureMovieStudioEnabled { get; set; } = false;

    public bool IsFeatureReviewsEnabled { get; set; } = false;
    public bool IsFeatureCommentsEnabled { get; set; } = false;
    public int CrunchyrollUpdateThresholdInDays { get; set; } = 1;
}