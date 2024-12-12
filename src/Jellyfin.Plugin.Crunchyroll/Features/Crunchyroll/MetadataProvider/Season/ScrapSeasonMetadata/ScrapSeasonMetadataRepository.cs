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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;

public class ScrapSeasonMetadataRepository : IScrapSeasonMetadataRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<ScrapSeasonMetadataRepository> _logger;

    public ScrapSeasonMetadataRepository(CrunchyrollDbContext dbContext,
        ILogger<ScrapSeasonMetadataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId seriesId, CultureInfo language, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == seriesId.ToString() &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get title metadata for item with id {SeriesId}", seriesId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public void UpdateTitleMetadata(Domain.Entities.TitleMetadata titleMetadata)
    {
        _dbContext.TitleMetadata.Update(titleMetadata);
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