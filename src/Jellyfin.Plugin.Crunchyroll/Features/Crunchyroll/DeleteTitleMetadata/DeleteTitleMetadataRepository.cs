using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;

public class DeleteTitleMetadataRepository : IDeleteTitleMetadataRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<DeleteTitleMetadataRepository> _logger;

    public DeleteTitleMetadataRepository(CrunchyrollDbContext dbContext, 
        ILogger<DeleteTitleMetadataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result> DeleteTitleMetadataAsync(string crunchyrollSeriesId, CultureInfo language)
    {
        try
        {
            var titleMetadata = await _dbContext.TitleMetadata
                .FirstOrDefaultAsync(x =>
                    x.CrunchyrollId == crunchyrollSeriesId &&
                    x.Language == language.Name);

            if (titleMetadata is null)
            {
                _logger.LogError("titlemetada for crunchyrollId {CrunchyrollId} was not found", crunchyrollSeriesId);
                return Result.Fail(ErrorCodes.ItemNotFound);
            }
            
            _dbContext.TitleMetadata.Remove(titleMetadata);
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete titlemetadata for {CrunchyrollId}", crunchyrollSeriesId);
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
            _logger.LogError(e, "Failed to save changes");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}