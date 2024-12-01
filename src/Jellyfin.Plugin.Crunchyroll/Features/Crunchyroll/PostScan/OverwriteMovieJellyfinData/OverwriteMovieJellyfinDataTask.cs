using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public class OverwriteMovieJellyfinDataTask : IPostMovieIdSetTask
{
    private readonly IOverwriteMovieJellyfinDataRepository _repository;
    private readonly ILogger<OverwriteMovieJellyfinDataTask> _logger;
    private readonly ISetEpisodeThumbnail _setEpisodeThumbnail;
    private readonly ILibraryManager _libraryManager;

    public OverwriteMovieJellyfinDataTask(IOverwriteMovieJellyfinDataRepository repository,
        ILogger<OverwriteMovieJellyfinDataTask> logger, ISetEpisodeThumbnail setEpisodeThumbnail,
        ILibraryManager libraryManager)
    {
        _repository = repository;
        _logger = logger;
        _setEpisodeThumbnail = setEpisodeThumbnail;
        _libraryManager = libraryManager;
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

        movie.Name = crunchyrollMovieEpisode.Title;
        movie.Overview = crunchyrollMovieEpisode.Description;
        movie.SetStudios([titleMetadataResult.Value.Studio]);

        var thumbnail = JsonSerializer.Deserialize<ImageSource>(crunchyrollMovieEpisode.Thumbnail)!;
        _ = await _setEpisodeThumbnail.GetAndSetThumbnailAsync(movie, thumbnail, cancellationToken);

        await _libraryManager.UpdateItemAsync(movie, movie.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }
}