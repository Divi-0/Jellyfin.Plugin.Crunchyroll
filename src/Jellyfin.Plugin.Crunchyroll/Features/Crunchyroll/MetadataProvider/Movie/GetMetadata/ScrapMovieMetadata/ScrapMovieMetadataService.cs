using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;

public class ScrapMovieMetadataService : IScrapMovieMetadataService
{
    private readonly IScrapSeriesMetadataService _scrapSeriesMetadataService;
    private readonly IScrapSeasonMetadataService _scrapSeasonMetadataService;
    private readonly IScrapEpisodeMetadataService _scrapEpisodeMetadataService;
    private readonly IScrapMovieMetadataRepository _repository;
    private readonly IScrapEpisodeCrunchyrollClient _crunchyrollEpisodesClient;
    private readonly ILogger<ScrapMovieMetadataService> _logger;

    public ScrapMovieMetadataService(IScrapSeriesMetadataService scrapSeriesMetadataService,
        IScrapSeasonMetadataService scrapSeasonMetadataService,
        IScrapEpisodeMetadataService scrapEpisodeMetadataService,
        IScrapMovieMetadataRepository repository,
        IScrapEpisodeCrunchyrollClient crunchyrollEpisodesClient,
        ILogger<ScrapMovieMetadataService> logger)
    {
        _scrapSeriesMetadataService = scrapSeriesMetadataService;
        _scrapSeasonMetadataService = scrapSeasonMetadataService;
        _scrapEpisodeMetadataService = scrapEpisodeMetadataService;
        _repository = repository;
        _crunchyrollEpisodesClient = crunchyrollEpisodesClient;
        _logger = logger;
    }
    
    public async Task<Result> ScrapMovieMetadataAsync(CrunchyrollId seriesId, CrunchyrollId seasonId, CrunchyrollId episodeId,
        CultureInfo language, CancellationToken cancellationToken)
    {
        var scrapSeriesResult = await _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(seriesId, language, cancellationToken);

        if (scrapSeriesResult.IsFailed)
        {
            return scrapSeriesResult;
        }

        var scrapSeasonResult = await _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(seriesId, language, cancellationToken);

        if (scrapSeasonResult.IsFailed)
        {
            return scrapSeasonResult;
        }

        var scrapEpisodeResult = await _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(seasonId, language, cancellationToken);

        if (scrapEpisodeResult.IsFailed && scrapEpisodeResult.Errors.First().Message != EpisodesErrorCodes.RequestFailed)
        {
            return scrapEpisodeResult;
        }
        
        return await HandleMovieEpisodeNotPresentInMetadataAsync(episodeId, seasonId, seriesId, language, 
            cancellationToken);
    }
    
    private async Task<Result> HandleMovieEpisodeNotPresentInMetadataAsync(CrunchyrollId crunchyrollEpisodeId, 
        CrunchyrollId crunchyrollSeasonId, CrunchyrollId crunchyrollSeriesId, CultureInfo language, 
        CancellationToken cancellationToken)
    {
        var titleMetadataResult =
            await _repository.GetTitleMetadataAsync(crunchyrollSeriesId, language, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return titleMetadataResult.ToResult();
        }

        var titleMetadata = titleMetadataResult.Value;

        if (titleMetadata is null)
        {
            _logger.LogDebug("TitleMetadata for series {SeriesId} not found, skipping...", crunchyrollSeriesId);
            return Result.Fail(ErrorCodes.NotFound);
        }
        
        var isEpisodeAlreadyScraped = titleMetadata.Seasons
            .SelectMany(x => x.Episodes)
            .Any(x => x.CrunchyrollId == crunchyrollEpisodeId);
        
        if (isEpisodeAlreadyScraped)
        {
            return Result.Ok();
        }

        var episodeResult = await _crunchyrollEpisodesClient.GetEpisodeAsync(crunchyrollEpisodeId, language, cancellationToken);

        if (episodeResult.IsFailed)
        {
            return episodeResult.ToResult();
        }

        var season = titleMetadata.Seasons.FirstOrDefault(x => x.CrunchyrollId == crunchyrollSeasonId);

        if (season is null)
        {
            season = new Domain.Entities.Season
            {
                CrunchyrollId = crunchyrollSeasonId,
                Title = episodeResult.Value.EpisodeMetadata.SeasonTitle,
                Identifier = string.Empty,
                SeasonNumber = episodeResult.Value.EpisodeMetadata.SeasonNumber,
                SeasonSequenceNumber = episodeResult.Value.EpisodeMetadata.SeasonSequenceNumber,
                SlugTitle = episodeResult.Value.EpisodeMetadata.SeriesSlugTitle,
                SeasonDisplayNumber = episodeResult.Value.EpisodeMetadata.SeasonDisplayNumber,
                Episodes = [],
                SeriesId = titleMetadata.Id,
                Language = language.Name
            };
            
            titleMetadata.Seasons.Add(season);
        }
        
        season.Episodes.Add(new Domain.Entities.Episode
        {
            CrunchyrollId = crunchyrollEpisodeId,
            Title = episodeResult.Value.Title,
            Description = episodeResult.Value.Description,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = episodeResult.Value.Images.Thumbnail.First().Last().Source,
                Height = episodeResult.Value.Images.Thumbnail.First().Last().Height,
                Width = episodeResult.Value.Images.Thumbnail.First().Last().Width
            }),
            EpisodeNumber = episodeResult.Value.EpisodeMetadata.Episode,
            SequenceNumber = episodeResult.Value.EpisodeMetadata.SequenceNumber,
            SlugTitle = episodeResult.Value.SlugTitle,
            SeasonId = season.Id,
            Language = language.Name
        });


        return await _repository.AddOrUpdateTitleMetadataAsync(titleMetadata, cancellationToken)
            .Bind(async () => await _repository.SaveChangesAsync(cancellationToken));
    }
}