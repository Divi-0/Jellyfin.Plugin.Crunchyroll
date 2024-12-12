using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;

public class GetSeasonRepository : IGetSeasonRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetSeasonRepository> _logger;

    public GetSeasonRepository(CrunchyrollDbContext dbContext,
        ILogger<GetSeasonRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<string?>> GetSeasonIdByNumberAsync(string crunchyrollTitleId, int seasonNumber, int duplicateCounter,
        CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .Include(x => x.Series)
                .Where(x =>
                        x.Series!.CrunchyrollId == crunchyrollTitleId &&
                        x.Language == language.Name &&
                        x.SeasonNumber == seasonNumber)
                .OrderBy(x => x.SeasonSequenceNumber)
                .Skip(duplicateCounter)
                .Select(x => x.CrunchyrollId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get season id for title id {TitleId}, seansonNumber {SeasonNumber}," +
                                "duplicateCounter {DuplicateCounter}, language {Language}", 
                crunchyrollTitleId,
                seasonNumber,
                duplicateCounter,
                language.Name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<Season>>> GetAllSeasonsAsync(string crunchyrollTitleId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .Include(x => x.Series)
                .Where(x =>
                    x.Series!.CrunchyrollId == crunchyrollTitleId &&
                    x.Language == language.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get all seasons for title id {TitleId}, language {Language}", 
                crunchyrollTitleId,
                language.Name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<string?>> GetSeasonIdByNameAsync(string crunchyrollTitleId, string name, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .Include(x => x.Series)
                .Where(x =>
                    x.Series!.CrunchyrollId == crunchyrollTitleId &&
                    x.Title.Replace(":", string.Empty) //Replace ':' to make folder names compatible
                        .Contains(name.Replace(":", string.Empty)) &&
                    x.Language == language.Name)
                .Select(x => x.CrunchyrollId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get season id for title id {TitleId}, name {Name}, language {Language}", 
                crunchyrollTitleId,
                name,
                language.Name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}