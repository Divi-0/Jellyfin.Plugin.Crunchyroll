using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;

public class GetSeriesImageInfosRepository : IGetSeriesImageInfosRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<GetSeriesImageInfosRepository> _logger;

    public GetSeriesImageInfosRepository(CrunchyrollDbContext dbContext,
        ILogger<GetSeriesImageInfosRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId seriesId, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CrunchyrollId == seriesId.ToString(), 
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error while getting titlemetadata for crunchyrollId {SeriesId}",
                seriesId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}