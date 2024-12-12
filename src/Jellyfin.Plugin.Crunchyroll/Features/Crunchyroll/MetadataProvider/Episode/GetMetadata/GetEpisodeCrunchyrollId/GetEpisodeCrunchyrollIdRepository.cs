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
}