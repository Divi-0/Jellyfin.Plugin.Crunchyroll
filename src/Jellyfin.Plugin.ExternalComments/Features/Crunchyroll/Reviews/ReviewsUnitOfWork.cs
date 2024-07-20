using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;

public sealed class ReviewsUnitOfWork : IAddReviewsSession, IGetReviewsSession
{
    private readonly string _dbFilePath;

    public ReviewsUnitOfWork(PluginConfiguration config)
    {
        _dbFilePath = config.LocalDatabasePath;

        if (string.IsNullOrWhiteSpace(_dbFilePath))
        {
            var location = typeof(ReviewsUnitOfWork).Assembly.Location;
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

    public ValueTask<Result<IReadOnlyList<ReviewItem>?>> GetReviewsForTitleIdAsync(string titleId)
    {
        using var db = new LiteDatabase(_dbFilePath);
        
        var reviewsCollection = db.GetCollection<TitleReviews>("reviews");
        var reviewsEntity = reviewsCollection.FindOne(x => x.TitleId == titleId);

        if (reviewsEntity is null)
        {
            return ValueTask.FromResult(Result.Ok<IReadOnlyList<ReviewItem>?>(null));
        }
        
        return ValueTask.FromResult(Result.Ok<IReadOnlyList<ReviewItem>?>(reviewsEntity.Reviews));
    }
}