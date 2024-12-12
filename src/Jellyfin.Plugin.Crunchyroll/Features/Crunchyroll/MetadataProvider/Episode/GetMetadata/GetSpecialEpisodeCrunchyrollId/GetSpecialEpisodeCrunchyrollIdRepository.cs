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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;

public class GetSpecialEpisodeCrunchyrollIdRepository : IGetSpecialEpisodeCrunchyrollIdRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetSpecialEpisodeCrunchyrollIdRepository> _logger;

    public GetSpecialEpisodeCrunchyrollIdRepository(CrunchyrollDbContext dbContext,
        ILogger<GetSpecialEpisodeCrunchyrollIdRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<CrunchyrollId?>> GetEpisodeIdByNameAsync(CrunchyrollId seriesId, string name, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .AsNoTracking()
                .Include(x => x.Season)
                .ThenInclude(x => x!.Series)
                .Where(x =>
                    x.Season!.Series!.CrunchyrollId == seriesId.ToString() &&
                    EF.Functions.Like(x.Title, $"%{name}%"))
                .Select(x => new CrunchyrollId(x.CrunchyrollId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error while getting episode id for series {SeriesId} and episode {Name}",
                seriesId,
                name);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}