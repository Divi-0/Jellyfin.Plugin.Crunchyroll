using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Persistence.Entities;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Persistence;

public sealed class CrunchyrollUnitOfWork : IAddReviewsSession, IGetReviewsSession
{
    private readonly string _dbFilePath;

    public CrunchyrollUnitOfWork(PluginConfiguration config)
    {
        _dbFilePath = config.LocalDatabasePath;

        if (string.IsNullOrWhiteSpace(_dbFilePath))
        {
            var location = typeof(CrunchyrollUnitOfWork).Assembly.Location;
            _dbFilePath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");
        }
    }
    
    public ValueTask AddReviewsForTitleIdAsync(string titleId, IReadOnlyList<ReviewItem> reviews)
    {
        using var db = new LiteDatabase(_dbFilePath);
        
        var reviewsCollection = db.GetCollection<TitleReviews>("reviews");

        var entity = new TitleReviews()
        {
            TitleId = titleId,
            Reviews = reviews,
        };
            
        reviewsCollection.Insert(entity);
        reviewsCollection.EnsureIndex(x => x.TitleId, true);

        return ValueTask.CompletedTask;
    }

    public ValueTask<Result<IReadOnlyList<ReviewItem>>> GetReviewsForTitleIdAsync(string titleId)
    {
        using var db = new LiteDatabase(_dbFilePath);
        
        var reviewsCollection = db.GetCollection<TitleReviews>("reviews");
        var reviewsEntity = reviewsCollection.FindOne(x => x.TitleId == titleId);

        if (reviewsEntity is null)
        {
            return ValueTask.FromResult<Result<IReadOnlyList<ReviewItem>>>(Result.Fail(GetReviewsErrorCodes.ReviewsNotFound));
        }
        
        return ValueTask.FromResult(Result.Ok(reviewsEntity.Reviews));
    }
}