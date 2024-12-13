using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews;

public class ReviewsRepistory : IAddReviewsRepistory, IGetReviewsRepository
{
    private readonly CrunchyrollDbContext _dbContext;
    private readonly ILogger<ReviewsRepistory> _logger;

    public ReviewsRepistory(CrunchyrollDbContext dbContext, ILogger<ReviewsRepistory> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Result> AddReviewsForTitleIdAsync(TitleReviews titleReviews, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext
                .Reviews
                .AddAsync(titleReviews, cancellationToken);

            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error during adding revies for titleId {TitleId}", 
                titleReviews.CrunchyrollSeriesId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<string?>> GetSeriesSlugTitle(CrunchyrollId seriesId, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.TitleMetadata
                .AsNoTracking()
                .Where(x =>
                    x.CrunchyrollId == seriesId.ToString())
                .Select(x => x.SlugTitle)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error while getting slug title for {SeriesId}", 
                seriesId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<ReviewItem>?>> GetReviewsForTitleIdAsync(string titleId, CultureInfo language,
        CancellationToken cancellationToken)
    {
        try
        {
            var reviews = await _dbContext
                .Reviews
                .AsNoTracking()
                .Where(x =>
                    x.CrunchyrollSeriesId == titleId &&
                    x.Language == language.Name)
                .Select(x => x.Reviews)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(reviews))
            {
                return Result.Ok<IReadOnlyList<ReviewItem>?>(null);
            }

            return Result.Ok(JsonSerializer.Deserialize<IReadOnlyList<ReviewItem>?>(reviews) ?? [])!;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown error during adding revies for titleId {TitleId}", 
                titleId);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save changes for reviews");
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}