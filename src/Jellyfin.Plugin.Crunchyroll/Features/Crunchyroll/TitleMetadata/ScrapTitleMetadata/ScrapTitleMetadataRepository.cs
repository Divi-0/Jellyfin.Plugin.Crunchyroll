using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public class ScrapTitleMetadataRepository : IScrapTitleMetadataRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<ScrapTitleMetadataRepository> _logger;

    public ScrapTitleMetadataRepository(CrunchyrollDbContext dbContext, 
        ILogger<ScrapTitleMetadataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(string titleId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == titleId &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get title metadata for item with id {TitleId}", titleId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result> AddOrUpdateTitleMetadata(Domain.Entities.TitleMetadata titleMetadata,
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