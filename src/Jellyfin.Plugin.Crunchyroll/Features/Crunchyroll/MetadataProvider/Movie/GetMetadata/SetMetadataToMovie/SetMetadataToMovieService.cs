using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;

public partial class SetMetadataToMovieService : ISetMetadataToMovieService
{
    private readonly ISetMetadataToMovieRepository _repository;
    private readonly ILogger<SetMetadataToMovieService> _logger;
    private readonly PluginConfiguration _config;

    public SetMetadataToMovieService(ISetMetadataToMovieRepository repository,
        ILogger<SetMetadataToMovieService> logger, PluginConfiguration config)
    {
        _repository = repository;
        _logger = logger;
        _config = config;
    }
    
    public async Task<Result<MediaBrowser.Controller.Entities.Movies.Movie>> SetMetadataToMovieAsync(CrunchyrollId seriesId, CrunchyrollId seasonId, CrunchyrollId episodeId,
        CultureInfo language, CancellationToken cancellationToken)
    {
        var titleMetadataResult = await _repository
            .GetTitleMetadataAsync(seriesId, language, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return titleMetadataResult.ToResult();
        }

        var titleMetadata = titleMetadataResult.Value;

        if (titleMetadata is null)
        {
            _logger.LogDebug("TitleMetadata for series {SeriesId} not found", seriesId);
            return Result.Fail(ErrorCodes.NotFound);
        }
        
        var crunchyrollMovieEpisode = titleMetadata.Seasons
            .SelectMany(x => x.Episodes)
            .FirstOrDefault(x => x.CrunchyrollId == episodeId);
        
        if (crunchyrollMovieEpisode is null)
        {
            _logger.LogError("No episode found for episode with id {EpisodeId} and seriesId {SeriesId}", episodeId, seriesId);
            return Result.Fail(ErrorCodes.NotFound);
        }

        var movieWithNewMetadata = new MediaBrowser.Controller.Entities.Movies.Movie();
        
        SetMovieTitle(movieWithNewMetadata, crunchyrollMovieEpisode);
        SetMovieOverview(movieWithNewMetadata, crunchyrollMovieEpisode);
        SetMovieStudios(movieWithNewMetadata, titleMetadata);
        
        return movieWithNewMetadata;
    }
    
    private void SetMovieTitle(MediaBrowser.Controller.Entities.Movies.Movie movie, 
        Domain.Entities.Episode crunchyrollMovieEpisode)
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

    private void SetMovieOverview(MediaBrowser.Controller.Entities.Movies.Movie movie, 
        Domain.Entities.Episode crunchyrollMovieEpisode)
    {
        if (!_config.IsFeatureMovieDescriptionEnabled)
        {
            return;
        }
        
        movie.Overview = crunchyrollMovieEpisode.Description;
    }

    private void SetMovieStudios(MediaBrowser.Controller.Entities.Movies.Movie movie, 
        Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureMovieStudioEnabled)
        {
            return;
        }
        
        movie.SetStudios([titleMetadata.Studio]);
    }
    
    [GeneratedRegex(@"\(.*\) (.*)")]
    private static partial Regex NameWithBracketsRegex();
}