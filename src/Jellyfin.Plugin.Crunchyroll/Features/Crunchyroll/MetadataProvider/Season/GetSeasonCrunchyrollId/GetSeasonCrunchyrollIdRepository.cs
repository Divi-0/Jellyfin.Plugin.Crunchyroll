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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;

public class GetSeasonCrunchyrollIdRepository : IGetSeasonCrunchyrollIdRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetSeasonCrunchyrollIdRepository> _logger;

    public GetSeasonCrunchyrollIdRepository(CrunchyrollDbContext dbContext,
        ILogger<GetSeasonCrunchyrollIdRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<CrunchyrollId?>> GetSeasonIdByNumberAsync(CrunchyrollId titleId, int seasonNumber, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .AsNoTracking()
                .Include(x => x.Series)
                .Where(x =>
                    x.Series!.CrunchyrollId == titleId.ToString() &&
                    x.Language == language.Name &&
                    x.SeasonNumber == seasonNumber)
                .OrderBy(x => x.SeasonSequenceNumber)
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get season id for title id {TitleId}, seansonNumber {SeasonNumber}," +
                                "language {Language}", 
                titleId,
                seasonNumber,
                language.Name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<CrunchyrollId?>> GetSeasonIdByNameAsync(CrunchyrollId crunchyrollTitleId, string name, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .AsNoTracking()
                .Include(x => x.Series)
                .Where(x =>
                    x.Series!.CrunchyrollId == crunchyrollTitleId.ToString() &&
                    x.Title.Replace(":", string.Empty) //Replace ':' to make folder names compatible
                        .Contains(name.Replace(":", string.Empty)) &&
                    x.Language == language.Name)
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
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
    
    public async Task<Result<Domain.Entities.Season[]>> GetAllSeasonsAsync(CrunchyrollId crunchyrollTitleId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .AsNoTracking()
                .Include(x => x.Series)
                .Where(x =>
                    x.Series!.CrunchyrollId == crunchyrollTitleId.ToString() &&
                    x.Language == language.Name)
                .ToArrayAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get all seasons for title id {TitleId}, language {Language}", 
                crunchyrollTitleId,
                language.Name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}