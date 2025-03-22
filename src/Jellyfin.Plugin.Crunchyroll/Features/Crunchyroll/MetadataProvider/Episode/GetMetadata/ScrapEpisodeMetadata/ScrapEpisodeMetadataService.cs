using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public class ScrapEpisodeMetadataService : IScrapEpisodeMetadataService
{
    private readonly IScrapEpisodeCrunchyrollClient _client;
    private readonly ILoginService _loginService;
    private readonly IScrapEpisodeMetadataRepository _repository;
    private readonly ILogger<ScrapEpisodeMetadataService> _logger;
    private readonly IScrapLockRepository _scrapLockRepository;
    private readonly IScrapMissingEpisodeService _scrapMissingEpisodeService;
    private readonly TimeProvider _timeProvider;
    private readonly PluginConfiguration _config;

    public ScrapEpisodeMetadataService(IScrapEpisodeCrunchyrollClient client,
        ILoginService loginService, IScrapEpisodeMetadataRepository repository,
        ILogger<ScrapEpisodeMetadataService> logger, IScrapLockRepository scrapLockRepository,
        IScrapMissingEpisodeService scrapMissingEpisodeService,
        TimeProvider timeProvider,
        PluginConfiguration config)
    {
        _client = client;
        _loginService = loginService;
        _repository = repository;
        _logger = logger;
        _scrapLockRepository = scrapLockRepository;
        _scrapMissingEpisodeService = scrapMissingEpisodeService;
        _timeProvider = timeProvider;
        _config = config;
    }
    
    public async Task<Result> ScrapEpisodeMetadataAsync(CrunchyrollId seasonId, CrunchyrollId? episodeId, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        if (!await _scrapLockRepository.AddLockAsync(seasonId))
        {
            _logger.LogDebug("Episode metadata for season {SeasonId} is up to date, skipping...", seasonId);

            if (episodeId is not null)
            {
                _ = await HandleMissingEpisode(episodeId, language, cancellationToken);
            }
            
            return Result.Ok();
        }
        
        var getSeasonResult = await _repository.GetSeasonAsync(seasonId, language, cancellationToken);

        if (getSeasonResult.IsFailed)
        {
            return getSeasonResult.ToResult();
        }

        var season = getSeasonResult.Value;

        if (season is null)
        {
            _logger.LogError("Failed to get season with id {SeasonId} and language {Language}",
                seasonId,
                language.Name);
            return Result.Fail(ErrorCodes.NotFound);
        }

        if (season.LastUpdatedAt.AddDays(_config.CrunchyrollUpdateThresholdInDays) > _timeProvider.GetUtcNow())
        {
            _logger.LogInformation("Not updating episodes from season {SeasonId}, threshold is not reached", seasonId);
            return Result.Ok();
        }

        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }

        var crunchyrollEpisodesResult = await _client.GetEpisodesAsync(seasonId, language, cancellationToken);

        if (crunchyrollEpisodesResult.IsFailed)
        {
            return crunchyrollEpisodesResult.ToResult();
        }
        
        var episodes = crunchyrollEpisodesResult.Value.Data
            .Select(x => x.ToEpisodeEntity(season.Id, language)).ToArray();
            
        if (season.Episodes.Count == 0)
        {
            season.Episodes.AddRange(episodes.AsSpan());
        }
        else
        {
            ApplyNewEpisodesToExistingEpisodes(season, episodes);
        }
        
        _repository.UpdateSeason(season);

        return await _repository.SaveChangesAsync(cancellationToken)
            .Bind(async () => await HandleMissingEpisode(episodeId, language, cancellationToken));
    }
    
    private static void ApplyNewEpisodesToExistingEpisodes(Domain.Entities.Season season, Domain.Entities.Episode[] episodes)
    {
        foreach (var currentListedCrunchyrollEpisode in episodes.AsSpan())
        {
            if (season.Episodes.All(x => x.CrunchyrollId != currentListedCrunchyrollEpisode.CrunchyrollId))
            {
                season.Episodes.Add(currentListedCrunchyrollEpisode);
            }
        }
    }

    private async Task<Result> HandleMissingEpisode(CrunchyrollId? episodeId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        if (episodeId is null)
        {
            return Result.Ok();
        }

        return await _scrapMissingEpisodeService.ScrapMissingEpisodeAsync(episodeId, language, cancellationToken);
    }
}