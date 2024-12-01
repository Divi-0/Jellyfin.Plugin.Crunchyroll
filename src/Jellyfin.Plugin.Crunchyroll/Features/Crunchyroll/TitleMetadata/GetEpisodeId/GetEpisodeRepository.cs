using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public class GetEpisodeRepository : IGetEpisodeRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetEpisodeRepository> _logger;

    public GetEpisodeRepository(CrunchyrollDbContext dbContext, ILogger<GetEpisodeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<EpisodeIdResult?>> GetEpisodeIdAsync(string crunchyrollSeasonId, string episodeIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .Include(x => x.Season)
                .Where(x =>
                    x.EpisodeNumber == episodeIdentifier &&
                    x.Season!.CrunchyrollId == crunchyrollSeasonId)
                .Select(x => new EpisodeIdResult(
                    x.CrunchyrollId,
                    x.SlugTitle))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get crunchyrollId for episode with crunchyroll seasonId {SeasonId}," +
                                " episodeIdentifier {EpisodeIdentifier}",
                crunchyrollSeasonId,
                episodeIdentifier);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<EpisodeIdResult?>> GetEpisodeIdByNameAsync(string crunchyrollSeasonId, string episodeName, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .Include(x => x.Season)
                .Where(x =>
                    EF.Functions.Like(x.Title, $"%{episodeName}%") &&
                    x.Season!.CrunchyrollId == crunchyrollSeasonId)
                .Select(x => new EpisodeIdResult(
                    x.CrunchyrollId,
                    x.SlugTitle))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get crunchyrollId for episode with crunchyroll seasonId {SeasonId}," +
                                " episodeName {EpisodeName}",
                crunchyrollSeasonId,
                episodeName);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}