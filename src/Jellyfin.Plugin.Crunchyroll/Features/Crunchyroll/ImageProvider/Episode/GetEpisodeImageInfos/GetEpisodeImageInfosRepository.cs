using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;

public class GetEpisodeImageInfosRepository : IGetEpisodeImageInfosRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetEpisodeImageInfosRepository> _logger;

    public GetEpisodeImageInfosRepository(CrunchyrollDbContext dbContext,
        ILogger<GetEpisodeImageInfosRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.Episode?>> GetEpisodeAsync(CrunchyrollId episodeId, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == episodeId.ToString(),
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get episode by id {EpisodeId}", episodeId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}