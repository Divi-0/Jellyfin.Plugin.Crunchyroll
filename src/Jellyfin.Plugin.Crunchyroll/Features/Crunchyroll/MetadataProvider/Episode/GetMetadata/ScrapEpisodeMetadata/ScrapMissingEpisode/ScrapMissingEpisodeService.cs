using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Extensions;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;

public class ScrapMissingEpisodeService : IScrapMissingEpisodeService
{
    private readonly IScrapMissingEpisodeRepository _repository;
    private readonly IScrapEpisodeCrunchyrollClient _episodeCrunchyrollClient;
    private readonly ILogger<ScrapMissingEpisodeService> _logger;

    public ScrapMissingEpisodeService(IScrapMissingEpisodeRepository repository,
        IScrapEpisodeCrunchyrollClient episodeCrunchyrollClient,
        ILogger<ScrapMissingEpisodeService> logger)
    {
        _repository = repository;
        _episodeCrunchyrollClient = episodeCrunchyrollClient;
        _logger = logger;
    }
    
    public async Task<Result> ScrapMissingEpisodeAsync(CrunchyrollId episodeId, CultureInfo language, 
        CancellationToken cancellationToken)
    {
        var episodeExistsResult = await _repository
            .EpisodeExistsAsync(episodeId, language, cancellationToken);

        if (episodeExistsResult.IsFailed)
        {
            return episodeExistsResult.ToResult();
        }

        if (episodeExistsResult.Value == true)
        {
            return Result.Ok();
        }
        
        var episodeResult = await _episodeCrunchyrollClient.GetEpisodeAsync(episodeId, language, cancellationToken);

        if (episodeResult.IsFailed)
        {
            return episodeResult.ToResult();
        }

        var episodeResponse = episodeResult.Value;

        var titleMetadataResult = await _repository
            .GetTitleMetadataAsync(episodeResponse.EpisodeMetadata.SeriesId, language, cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            return titleMetadataResult.ToResult();
        }

        var titleMetadata = titleMetadataResult.Value;

        if (titleMetadata is null)
        {
            titleMetadata = new TitleMetadata
            {
                Id = Guid.NewGuid(),
                CrunchyrollId = episodeResponse.EpisodeMetadata.SeriesId,
                Title = episodeResponse.EpisodeMetadata.SeriesTitle,
                SlugTitle = episodeResponse.EpisodeMetadata.SeriesSlugTitle,
                Description = string.Empty,
                Language = language.Name,
                Studio = string.Empty,
                Rating = 0,
                PosterTall = string.Empty,
                PosterWide = string.Empty
            };
        }

        var season = titleMetadata.Seasons
            .FirstOrDefault(x => x.CrunchyrollId == episodeResponse.EpisodeMetadata.SeasonId);

        if (season is null)
        {
            season = new Domain.Entities.Season
            {
                Id = Guid.NewGuid(),
                CrunchyrollId = episodeResponse.EpisodeMetadata.SeasonId,
                Title = episodeResponse.EpisodeMetadata.SeasonTitle,
                SlugTitle = episodeResponse.EpisodeMetadata.SeasonSlugTitle,
                Identifier = string.Empty,
                Language = language.Name,
                SeasonNumber = episodeResponse.EpisodeMetadata.SeasonNumber,
                SeasonSequenceNumber = episodeResponse.EpisodeMetadata.SeasonSequenceNumber,
                SeasonDisplayNumber = episodeResponse.EpisodeMetadata.SeasonDisplayNumber,
                SeriesId = titleMetadata.Id
            };
            
            titleMetadata.Seasons.Add(season);
        }
        
        season.Episodes.Add(new Domain.Entities.Episode
        {
            Id = Guid.NewGuid(),
            CrunchyrollId = episodeResponse.Id,
            Title = episodeResponse.Title,
            Description = episodeResponse.Description,
            Language = language.Name,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = episodeResponse.Images.Thumbnail.First().Last().Source,
                Height = episodeResponse.Images.Thumbnail.First().Last().Height,
                Width = episodeResponse.Images.Thumbnail.First().Last().Width
            }),
            EpisodeNumber = episodeResponse.EpisodeMetadata.Episode,
            SequenceNumber = episodeResponse.EpisodeMetadata.SequenceNumber,
            SlugTitle = episodeResponse.SlugTitle,
            SeasonId = season.Id,
        });

        return await _repository.AddOrUpdateTitleMetadataAsync(titleMetadata, cancellationToken)
            .Bind(async () => await _repository.SaveChangesAsync(cancellationToken));
    }
}