using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public sealed class CrunchyrollUnitOfWork : IAddReviewsSession, IGetReviewsSession, IGetAvatarSession
{
    private readonly string _dbFilePath;
    private readonly SemaphoreSlim _semaphore;
    
    private const string ReviewsCollectionName = "reviews";
    private const string AvatarImageFileStorageName = "avatarImageFiles";
    private const string AvatarImageChunkName = "avatarImageChunks";

    public CrunchyrollUnitOfWork(PluginConfiguration config)
    {
        _dbFilePath = config.LocalDatabasePath;
        _semaphore = new SemaphoreSlim(1, 1);

        if (string.IsNullOrWhiteSpace(_dbFilePath))
        {
            var location = typeof(CrunchyrollUnitOfWork).Assembly.Location;
            _dbFilePath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");
        }
    }
    
    public ValueTask AddReviewsForTitleIdAsync(string titleId, IReadOnlyList<ReviewItem> reviews)
    {
        _semaphore.Wait();

        try
        {
            using var db = new LiteDatabase(_dbFilePath);

            var reviewsCollection = db.GetCollection<TitleReviews>(ReviewsCollectionName);

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

            var reviewsCollection = db.GetCollection<TitleReviews>(ReviewsCollectionName);
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

    public ValueTask AddAvatarImageAsync(string url, Stream imageStream)
    {
        _semaphore.Wait();

        try
        {
            using var db = new LiteDatabase(_dbFilePath);
            var fileStorage = db.GetStorage<string>(AvatarImageFileStorageName, AvatarImageChunkName);
            
            fileStorage.Upload(url, url, imageStream);
            
            return ValueTask.CompletedTask;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<bool> ExistsAsync(string url)
    {
        _semaphore.Wait();

        try
        {
            using var db = new LiteDatabase(_dbFilePath);
            var fileStorage = db.GetStorage<string>(AvatarImageFileStorageName, AvatarImageChunkName);

            var exists = fileStorage.Exists(url);
            
            return ValueTask.FromResult(exists);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<Stream?> GetAvatarImageAsync(string url)
    {
        _semaphore.Wait();

        try
        {
            using var db = new LiteDatabase(_dbFilePath);
            var fileStorage = db.GetStorage<string>(AvatarImageFileStorageName, AvatarImageChunkName);

            var fileInfo = fileStorage.FindById(url);

            if (fileInfo is null)
            {
                return ValueTask.FromResult<Stream?>(null);
            }
            
            return ValueTask.FromResult<Stream?>(fileInfo.OpenRead());
        }
        finally
        {
            _semaphore.Release();
        }
    }
}