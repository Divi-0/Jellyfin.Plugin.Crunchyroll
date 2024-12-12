using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public class OverwriteMovieJellyfinDataRepository : IOverwriteMovieJellyfinDataRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<OverwriteMovieJellyfinDataRepository> _logger;

    public OverwriteMovieJellyfinDataRepository(CrunchyrollDbContext dbContext,
        ILogger<OverwriteMovieJellyfinDataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(string seriesId, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == seriesId &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get title metadata for {CrunchyrollSeriesId} in language {Language}", 
                seriesId, language.Name);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }
}