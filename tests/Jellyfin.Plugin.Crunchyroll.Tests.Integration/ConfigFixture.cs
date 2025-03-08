using Jellyfin.Plugin.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration;

public class ConfigFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        while (CrunchyrollPlugin.Instance is null)
        {
        }

        var config = CrunchyrollPlugin.Instance.ServiceProvider.GetRequiredService<PluginConfiguration>();
        
        config.IsFeatureSeriesTitleEnabled = true;
        config.IsFeatureSeriesDescriptionEnabled = true;
        config.IsFeatureSeriesStudioEnabled = true;
        config.IsFeatureSeriesRatingsEnabled = true;
        
        config.IsFeatureSeasonTitleEnabled = true;
        config.IsFeatureSeasonOrderByCrunchyrollOrderEnabled = true;
        
        config.IsFeatureEpisodeTitleEnabled = true;
        config.IsFeatureEpisodeDescriptionEnabled = true;
        config.IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = true;
        
        config.IsFeatureMovieTitleEnabled = true;
        config.IsFeatureMovieDescriptionEnabled = true;
        config.IsFeatureMovieStudioEnabled = true;
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}