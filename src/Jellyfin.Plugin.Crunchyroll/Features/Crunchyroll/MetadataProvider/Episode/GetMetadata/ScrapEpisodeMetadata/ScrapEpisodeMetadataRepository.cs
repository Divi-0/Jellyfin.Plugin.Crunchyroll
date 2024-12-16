using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public class ScrapEpisodeMetadataRepository : IScrapEpisodeMetadataRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<ScrapEpisodeMetadataRepository> _logger;

    public ScrapEpisodeMetadataRepository(CrunchyrollDbContext dbContext,
        ILogger<ScrapEpisodeMetadataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Domain.Entities.Season?>> GetSeasonAsync(CrunchyrollId seasonId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .Include(x => x.Episodes)
                .FirstOrDefaultAsync(x =>
                    x.CrunchyrollId == seasonId.ToString() &&
                    x.Language == language.Name, 
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown database error, while getting season");
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public void UpdateSeason(Domain.Entities.Season season)
    {
        _dbContext.Seasons.Update(season);
    }
    
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown database error, while saving");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}