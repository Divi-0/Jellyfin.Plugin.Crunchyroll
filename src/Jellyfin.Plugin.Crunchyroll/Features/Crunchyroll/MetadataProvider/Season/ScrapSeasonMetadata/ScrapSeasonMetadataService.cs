using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;

public class ScrapSeasonMetadataService : IScrapSeasonMetadataService
{
    private readonly ILogger<ScrapSeasonMetadataService> _logger;
    private readonly IScrapSeasonMetadataRepository _repository;
    private readonly ICrunchyrollSeasonsClient _client;
    private readonly ILoginService _loginService;
    private readonly IScrapLockRepository _scrapLockRepository;

    private static readonly ConcurrentDictionary<CrunchyrollId, SemaphoreSlim> SemaphoreSlims =
        new ConcurrentDictionary<CrunchyrollId, SemaphoreSlim>();

    public ScrapSeasonMetadataService(ILogger<ScrapSeasonMetadataService> logger, 
        IScrapSeasonMetadataRepository repository, ICrunchyrollSeasonsClient client,
        ILoginService loginService, IScrapLockRepository scrapLockRepository)
    {
        _logger = logger;
        _repository = repository;
        _client = client;
        _loginService = loginService;
        _scrapLockRepository = scrapLockRepository;
    }
    
    public async Task<Result> ScrapSeasonMetadataAsync(CrunchyrollId seriesId, CultureInfo language, CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(1, 1);

        //if already exist wait for completion of other task and then exit
        if (!SemaphoreSlims.TryAdd(seriesId, semaphore))
        {
            SemaphoreSlims.TryGetValue(seriesId, out var existingSemaphore);

            if (existingSemaphore is not null)
            {
                await existingSemaphore.WaitAsync(cancellationToken);
            }
            
            return Result.Ok();
        }

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!await _scrapLockRepository.AddLockAsync(seriesId))
            {
                _logger.LogDebug("Season metadata for series {SeriesId} is up to date, skipping...", seriesId);
                return Result.Ok();
            }
            
            var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

            if (loginResult.IsFailed)
            {
                return loginResult;
            }

            var titleMetadataResult = await _repository.GetTitleMetadataAsync(seriesId, language, cancellationToken);

            if (titleMetadataResult.IsFailed)
            {
                return titleMetadataResult.ToResult();
            }

            var titleMetadata = titleMetadataResult.Value;

            if (titleMetadata is null)
            {
                _logger.LogError("TitleMetadata for id {SeriesId} & language {Language} not found", seriesId,
                    language.Name);
                return Result.Fail(ErrorCodes.NotFound);
            }

            var crunchyrollSeasonsResult = await _client.GetSeasonsAsync(seriesId, language, cancellationToken);

            if (crunchyrollSeasonsResult.IsFailed)
            {
                return crunchyrollSeasonsResult.ToResult();
            }

            var seasons = crunchyrollSeasonsResult.Value.Data.Select(x =>
                x.ToSeasonEntity(titleMetadata.Id, language)).ToList();

            if (seasons.Count == 0)
            {
                titleMetadata.Seasons.AddRange(seasons);
            }
            else
            {
                ApplyNewSeasonsToExistingSeasons(titleMetadata, seasons);
            }

            _repository.UpdateTitleMetadata(titleMetadata);
            return await _repository.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            semaphore.Release();
            SemaphoreSlims.TryRemove(seriesId, out _);
        }
    }
    
    private static void ApplyNewSeasonsToExistingSeasons(Domain.Entities.TitleMetadata titleMetadata, List<Domain.Entities.Season> newSeasons)
    {
        foreach (var newSeason in newSeasons)
        {
            if (titleMetadata.Seasons.All(x => x.CrunchyrollId != newSeason.CrunchyrollId))
            {
                titleMetadata.Seasons.Add(newSeason);
            }
        }
    }
}