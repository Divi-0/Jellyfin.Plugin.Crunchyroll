using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;

public class OverwriteSeasonJellyfinDataRepository : IOverwriteSeasonJellyfinDataRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<OverwriteSeasonJellyfinDataRepository> _logger;

    public OverwriteSeasonJellyfinDataRepository(CrunchyrollDbContext dbContext,
        ILogger<OverwriteSeasonJellyfinDataRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Season?>> GetSeasonAsync(string crunchyrollSeasonId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == crunchyrollSeasonId &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get season {CrunchyrollSeasonId} in language {Language}", 
                crunchyrollSeasonId, language.Name);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }
}