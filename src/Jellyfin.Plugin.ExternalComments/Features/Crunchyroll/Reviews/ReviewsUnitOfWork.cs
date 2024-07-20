using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;

public sealed class ReviewsUnitOfWork : IAddReviewsSession, IGetReviewsSession
{
    private readonly string _dbFilePath;
    private readonly SemaphoreSlim _semaphore;

    public ReviewsUnitOfWork(PluginConfiguration config)
    {
        _dbFilePath = config.LocalDatabasePath;
        _semaphore = new SemaphoreSlim(1, 1);

        if (string.IsNullOrWhiteSpace(_dbFilePath))
        {
            var location = typeof(ReviewsUnitOfWork).Assembly.Location;
            _dbFilePath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");
        }
    }
    
    public ValueTask AddReviewsForTitleIdAsync(string titleId, IReadOnlyList<ReviewItem> reviews)
    {
        _semaphore.Wait();

        try
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
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<Result<IReadOnlyList<ReviewItem>?>> GetReviewsForTitleIdAsync(string titleId)
    {
        _semaphore.Wait();

        try
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
        finally
        {
            _semaphore.Release();
        }
    }
}