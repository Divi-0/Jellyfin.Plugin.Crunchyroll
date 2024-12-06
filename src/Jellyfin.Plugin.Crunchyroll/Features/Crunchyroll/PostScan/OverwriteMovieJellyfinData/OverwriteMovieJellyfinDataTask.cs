using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public partial class OverwriteMovieJellyfinDataTask : IPostMovieIdSetTask
{
    private readonly IOverwriteMovieJellyfinDataRepository _repository;
    private readonly ILogger<OverwriteMovieJellyfinDataTask> _logger;
    private readonly ISetEpisodeThumbnail _setEpisodeThumbnail;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;

    public OverwriteMovieJellyfinDataTask(IOverwriteMovieJellyfinDataRepository repository,
        ILogger<OverwriteMovieJellyfinDataTask> logger, ISetEpisodeThumbnail setEpisodeThumbnail,
        ILibraryManager libraryManager, PluginConfiguration config)
    {
        _repository = repository;
        _logger = logger;
        _setEpisodeThumbnail = setEpisodeThumbnail;
        _libraryManager = libraryManager;
        _config = config;
    }
    
    public async Task RunAsync(Movie movie, CancellationToken cancellationToken)
    {
        var hasSeriesId = movie.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, 
            out var seriesId) && !string.IsNullOrWhiteSpace(seriesId);
        
        var hasEpisodeId = movie.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, 
            out var episodeId) && !string.IsNullOrWhiteSpace(episodeId);

        if (!hasSeriesId || !hasEpisodeId)
        {
            _logger.LogDebug("Movie with name {Name} has no crunchyroll id. Skipping...", movie.FileNameWithoutExtension);
            return;
        }

        var titleMetadataResult = await _repository.GetTitleMetadataAsync(seriesId!, 
            movie.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return;
        }
        
        if (titleMetadataResult.Value is null)
        {
            _logger.LogError("No titleMetadata found for seriesId {SeriesId}", seriesId);
            return;
        }

        var crunchyrollMovieEpisode = titleMetadataResult.Value.Seasons
            .SelectMany(x => x.Episodes)
            .FirstOrDefault(x => x.CrunchyrollId == episodeId);

        if (crunchyrollMovieEpisode is null)
        {
            _logger.LogError("No episode found for episode with id {EpisodeId} and seriesId {SeriesId}", episodeId, seriesId);
            return;
        }

        SetMovieTitle(movie, crunchyrollMovieEpisode);
        SetMovieOverview(movie, crunchyrollMovieEpisode);
        SetMovieStudios(movie, titleMetadataResult.Value);
        
        await SetMovieThumbnailAsync(movie, crunchyrollMovieEpisode, cancellationToken);

        await _libraryManager.UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private void SetMovieTitle(Movie movie, Episode crunchyrollMovieEpisode)
    {
        if (!_config.IsFeatureMovieTitleEnabled)
        {
            return;
        }
        
        var match = NameWithBracketsRegex().Match(crunchyrollMovieEpisode.Title);
        movie.Name = match.Success 
            ? match.Groups[1].Value 
            : crunchyrollMovieEpisode.Title;
    }

    private void SetMovieOverview(Movie movie, Episode crunchyrollMovieEpisode)
    {
        if (!_config.IsFeatureMovieDescriptionEnabled)
        {
            return;
        }
        
        movie.Overview = crunchyrollMovieEpisode.Description;
    }

    private void SetMovieStudios(Movie movie, TitleMetadata.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureMovieStudioEnabled)
        {
            return;
        }
        
        movie.SetStudios([titleMetadata.Studio]);
    }
    
    private async Task SetMovieThumbnailAsync(Movie movie, Episode crunchyrollMovieEpisode,
        CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureMovieThumbnailImageEnabled)
        {
            return;
        }
        
        var thumbnail = JsonSerializer.Deserialize<ImageSource>(crunchyrollMovieEpisode.Thumbnail)!;
        _ = await _setEpisodeThumbnail.GetAndSetThumbnailAsync(movie, thumbnail, cancellationToken);
    }
    
    [GeneratedRegex(@"\(.*\) (.*)")]
    private static partial Regex NameWithBracketsRegex();
}