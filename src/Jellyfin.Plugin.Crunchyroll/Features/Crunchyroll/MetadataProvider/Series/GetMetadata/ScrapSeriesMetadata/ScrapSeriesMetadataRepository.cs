using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;

public sealed class ScrapSeriesMetadataRepository : ScrapBaseRepository, IScrapSeriesMetadataRepository
{
    public ScrapSeriesMetadataRepository(CrunchyrollDbContext dbContext, 
        ILogger<ScrapSeriesMetadataRepository> logger, TimeProvider timeProvider) : base(dbContext, logger, timeProvider)
    {
    }
    
    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId titleId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await DbContext.TitleMetadata
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == titleId.ToString() &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to get title metadata for item with id {TitleId}", titleId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result> AddOrUpdateTitleMetadata(Domain.Entities.TitleMetadata titleMetadata,
        CancellationToken cancellationToken)
    {
        try
        {
            var changesForTitleMetadata = DbContext.ChangeTracker.Entries<Domain.Entities.TitleMetadata>()
                .FirstOrDefault(x => x.Entity.Equals(titleMetadata));
            
            if (changesForTitleMetadata is null || changesForTitleMetadata.State == EntityState.Detached)
            {
                await DbContext.TitleMetadata
                    .AddAsync(titleMetadata, cancellationToken);
            }
            else
            {
                DbContext.TitleMetadata
                    .Update(titleMetadata);
            }
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to get title metadata for item with id {TitleId}", 
                titleMetadata.CrunchyrollId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}