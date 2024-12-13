using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;

public class CrunchyrollMovieGetMetadataService : ICrunchyrollMovieGetMetadataService
{
    private readonly IGetMovieCrunchyrollIdService _getMovieCrunchyrollIdService;
    private readonly IScrapMovieMetadataService _scrapMovieMetadataService;
    private readonly ISetMetadataToMovieService _setMetadataToMovieService;
    private readonly ILogger<CrunchyrollMovieGetMetadataService> _logger;
    private readonly ILoginService _loginService;

    public CrunchyrollMovieGetMetadataService(
        IGetMovieCrunchyrollIdService getMovieCrunchyrollIdService,
        IScrapMovieMetadataService scrapMovieMetadataService,
        ISetMetadataToMovieService setMetadataToMovieService,
        ILogger<CrunchyrollMovieGetMetadataService> logger,
        ILoginService loginService)
    {
        _getMovieCrunchyrollIdService = getMovieCrunchyrollIdService;
        _scrapMovieMetadataService = scrapMovieMetadataService;
        _setMetadataToMovieService = setMetadataToMovieService;
        _logger = logger;
        _loginService = loginService;
    }
    
    public async Task<MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>> GetMetadataAsync(MovieInfo info, CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return FailedResult;
        }

        var fileName = Path.GetFileNameWithoutExtension(info.Path);
        var language = info.GetPreferredMetadataCultureInfo();

        var seriesId = info.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);
        var seasonId = info.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeasonId);
        var episodeId = info.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (string.IsNullOrWhiteSpace(seriesId) ||
            string.IsNullOrWhiteSpace(seasonId) ||
            string.IsNullOrWhiteSpace(episodeId))
        {
            var getCrunchyrollIdResult = await _getMovieCrunchyrollIdService
                .GetCrunchyrollIdAsync(fileName, language, cancellationToken);

            if (getCrunchyrollIdResult.IsFailed)
            {
                return FailedResult;
            }

            if (getCrunchyrollIdResult.Value is null)
            {
                _logger.LogDebug("Crunchyroll ids for movie {Path} not found", info.Path);
                return FailedResult;
            }

            seriesId = getCrunchyrollIdResult.Value.SeriesId;
            seasonId = getCrunchyrollIdResult.Value.SeasonId;
            episodeId = getCrunchyrollIdResult.Value.EpisodeId;
        }

        //ignore result
        _ = await _scrapMovieMetadataService.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId,
            language, cancellationToken);

        var setMetadataResult = await _setMetadataToMovieService
            .SetMetadataToMovieAsync(seriesId, seasonId, episodeId,
                language, cancellationToken);

        if (setMetadataResult.IsFailed)
        {
            return FailedResult;
        }

        var movieWithNewMetadata = setMetadataResult.Value;
        movieWithNewMetadata.ProviderIds[CrunchyrollExternalKeys.SeriesId] = seriesId;
        movieWithNewMetadata.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonId;
        movieWithNewMetadata.ProviderIds[CrunchyrollExternalKeys.EpisodeId] = episodeId;

        return new MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>
        {
            HasMetadata = true,
            Item = setMetadataResult.Value
        };
    }

    private static MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie> FailedResult
        => new()
        {
            HasMetadata = false,
            Item = new MediaBrowser.Controller.Entities.Movies.Movie()
        };
}