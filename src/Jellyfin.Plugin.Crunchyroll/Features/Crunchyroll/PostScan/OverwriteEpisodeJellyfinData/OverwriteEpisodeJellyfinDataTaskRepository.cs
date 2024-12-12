using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

public class OverwriteEpisodeJellyfinDataTaskRepository : IOverwriteEpisodeJellyfinDataTaskRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<OverwriteEpisodeJellyfinDataTaskRepository> _logger;

    public OverwriteEpisodeJellyfinDataTaskRepository(CrunchyrollDbContext dbContext, 
        ILogger<OverwriteEpisodeJellyfinDataTaskRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Episode?>> GetEpisodeAsync(string crunchyrollEpisodeId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == crunchyrollEpisodeId &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get episode {CrunchyrollEpisodeId} in language {Language}", 
                crunchyrollEpisodeId, language.Name);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }
}