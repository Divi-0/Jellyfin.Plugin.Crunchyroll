using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;

public class ScrapMissingEpisodeRepository : IScrapMissingEpisodeRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<ScrapMissingEpisodeRepository> _logger;

    public ScrapMissingEpisodeRepository(CrunchyrollDbContext dbContext,
        ILogger<ScrapMissingEpisodeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<bool>> EpisodeExistsAsync(CrunchyrollId episodeId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes.AnyAsync(x =>
                x.CrunchyrollId == episodeId.ToString() &&
                x.Language == language.Name, 
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to check if episode exists for id {EpisodeId}", episodeId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId titleId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .Include(x => x.Seasons)
                .ThenInclude(x => x.Episodes)
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == titleId.ToString() &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get title metadata for item with id {TitleId}", titleId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result> AddOrUpdateTitleMetadataAsync(Domain.Entities.TitleMetadata titleMetadata,
        CancellationToken cancellationToken)
    {
        try
        {
            var changesForTitleMetadata = _dbContext.ChangeTracker.Entries<Domain.Entities.TitleMetadata>()
                .FirstOrDefault(x => x.Entity.Equals(titleMetadata));
            
            if (changesForTitleMetadata is null || changesForTitleMetadata.State == EntityState.Detached)
            {
                await _dbContext.TitleMetadata
                    .AddAsync(titleMetadata, cancellationToken);
            }
            else
            {
                _dbContext.TitleMetadata
                    .Update(titleMetadata);
            }
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get title metadata for item with id {TitleId}", 
                titleMetadata.CrunchyrollId);
            return Result.Fail(ErrorCodes.Internal);
        }
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