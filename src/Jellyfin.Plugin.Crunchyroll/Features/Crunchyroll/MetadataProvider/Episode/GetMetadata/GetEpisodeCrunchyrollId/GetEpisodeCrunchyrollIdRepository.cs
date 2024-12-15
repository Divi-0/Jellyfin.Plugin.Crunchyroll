using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;

public class GetEpisodeCrunchyrollIdRepository : IGetEpisodeCrunchyrollIdRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetEpisodeCrunchyrollIdRepository> _logger;

    public GetEpisodeCrunchyrollIdRepository(CrunchyrollDbContext dbContext,
        ILogger<GetEpisodeCrunchyrollIdRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<CrunchyrollId?>> GetEpisodeIdByName(CrunchyrollId seasonId, string name, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .Include(x => x.Season)
                .Where(x =>
                    EF.Functions.Like(x.Title, $"%{name}%") &&
                    x.Season!.CrunchyrollId == seasonId.ToString())
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get crunchyrollId for episode with crunchyroll seasonId {SeasonId}," +
                                " episodeName {EpisodeName}",
                seasonId,
                name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<CrunchyrollId?>> GetEpisodeIdByNumber(CrunchyrollId seasonId, string episodeNumber, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .Include(x => x.Season)
                .Where(x =>
                    x.EpisodeNumber == episodeNumber &&
                    x.Season!.CrunchyrollId == seasonId.ToString())
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get crunchyrollId for episode with crunchyroll seasonId {SeasonId}," +
                                " episodeNumber {EpisodeNumber}",
                seasonId,
                episodeNumber);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<CrunchyrollId?>> GetDuplicateNextSeasonIdAsync(CrunchyrollId seasonId, CrunchyrollId seriesId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Seasons
                .Include(x => x.Series)
                .Where(x =>
                    x.SeasonNumber == _dbContext.Seasons
                        .Where(y => y.CrunchyrollId == seasonId.ToString())
                        .Select(y => y.SeasonNumber)
                        .FirstOrDefault() &&
                    x.Series!.CrunchyrollId == seriesId.ToString() &&
                    x.CrunchyrollId != seasonId.ToString())
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get crunchyrollId for duplicate season with crunchyroll seasonId {SeasonId}",
                seasonId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<CrunchyrollId?>> GetEpisodeIdByNumberDuplicateNextSeasonAsync(CrunchyrollId seasonId, 
        CrunchyrollId seriesId, string episodeNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .Include(x => x.Season)
                .ThenInclude(x => x!.Series)
                .Where(x =>
                    x.EpisodeNumber == episodeNumber &&
                    x.Season!.SeasonNumber == _dbContext.Seasons
                        .Where(y => y.CrunchyrollId == seasonId.ToString())
                        .Select(y => y.SeasonNumber)
                        .FirstOrDefault() &&
                    x.Season!.Series!.CrunchyrollId == seriesId.ToString())
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get crunchyrollId for episode on duplicate season with crunchyroll seasonId {SeasonId}," +
                                " episodeNumber {EpisodeNumber}",
                seasonId,
                episodeNumber);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}