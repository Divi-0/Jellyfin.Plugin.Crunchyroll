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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;

public class SetMetadataToSeasonRepository : ISetMetadataToSeasonRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<SetMetadataToSeasonRepository> _logger;

    public SetMetadataToSeasonRepository(CrunchyrollDbContext dbContext,
        ILogger<SetMetadataToSeasonRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.Season?>> GetSeasonAsync(CrunchyrollId seasonId, CultureInfo language, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == seasonId.ToString() &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get season with id {SeasonId} & language {Language}", 
                seasonId, 
                language.Name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}