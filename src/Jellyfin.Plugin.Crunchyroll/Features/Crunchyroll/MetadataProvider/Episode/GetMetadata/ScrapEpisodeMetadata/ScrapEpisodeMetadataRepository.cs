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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public class ScrapEpisodeMetadataRepository : ScrapBaseRepository, IScrapEpisodeMetadataRepository
{
    public ScrapEpisodeMetadataRepository(CrunchyrollDbContext dbContext,
        ILogger<ScrapEpisodeMetadataRepository> logger, TimeProvider timeProvider) : base(dbContext, logger, timeProvider)
    {
    }

    public async Task<Result<Domain.Entities.Season?>> GetSeasonAsync(CrunchyrollId seasonId, CultureInfo language, CancellationToken cancellationToken)
    {
        try
        {
            return await DbContext.Seasons
                .Include(x => x.Episodes)
                .FirstOrDefaultAsync(x =>
                    x.CrunchyrollId == seasonId.ToString() &&
                    x.Language == language.Name, 
                    cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unknown database error, while getting season");
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public void UpdateSeason(Domain.Entities.Season season)
    {
        DbContext.Seasons.Update(season);
    }
}