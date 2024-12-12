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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;

public class SetMetadataToEpisodeRepository : ISetMetadataToEpisodeRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<SetMetadataToEpisodeRepository> _logger;

    public SetMetadataToEpisodeRepository(CrunchyrollDbContext dbContext,
        ILogger<SetMetadataToEpisodeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result<Domain.Entities.Episode?>> GetEpisodeAsync(CrunchyrollId episodeId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Episodes
                .AsNoTracking()
                .Include(x => x.Season)
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == episodeId.ToString() &&
                        x.Language == language.Name, 
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown database error, while getting episode");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}