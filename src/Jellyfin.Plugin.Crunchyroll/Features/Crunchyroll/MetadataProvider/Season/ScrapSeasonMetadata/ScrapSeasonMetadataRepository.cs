using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;

public class ScrapSeasonMetadataRepository : ScrapBaseRepository, IScrapSeasonMetadataRepository
{

    public ScrapSeasonMetadataRepository(CrunchyrollDbContext dbContext,
        ILogger<ScrapSeasonMetadataRepository> logger, TimeProvider timeProvider) : base(dbContext, logger, timeProvider)
    {
    }

    public async Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId seriesId, CultureInfo language, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await DbContext.TitleMetadata
                .FirstOrDefaultAsync(x =>
                        x.CrunchyrollId == seriesId.ToString() &&
                        x.Language == language.Name,
                    cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to get title metadata for item with id {SeriesId}", seriesId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public void UpdateTitleMetadata(Domain.Entities.TitleMetadata titleMetadata)
    {
        DbContext.TitleMetadata.Update(titleMetadata);
    }
}