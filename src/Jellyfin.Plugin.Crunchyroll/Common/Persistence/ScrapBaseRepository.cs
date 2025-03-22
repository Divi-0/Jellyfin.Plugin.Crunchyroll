using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Common.Persistence;

public abstract class ScrapBaseRepository : ISaveChanges
{
    protected readonly CrunchyrollDbContext DbContext;
    protected readonly ILogger Logger;
    private readonly TimeProvider _timeProvider;

    protected ScrapBaseRepository(CrunchyrollDbContext dbContext, 
        ILogger logger, TimeProvider timeProvider)
    {
        DbContext = dbContext;
        Logger = logger;
        _timeProvider = timeProvider;
    }
    
    public virtual async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var baseEntityEntry in DbContext.ChangeTracker.Entries<CrunchyrollBaseEntity>())
        {
            baseEntityEntry.Entity.LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        }
        
        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unknown database error, while saving");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}