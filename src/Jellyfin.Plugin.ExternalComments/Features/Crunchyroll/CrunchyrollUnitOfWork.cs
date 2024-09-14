using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using LiteDB;
using Polly;
using Polly.Retry;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public sealed class CrunchyrollUnitOfWork : 
    IAddReviewsSession, 
    IGetReviewsSession, 
    IGetAvatarSession,
    IScrapTitleMetadataSession,
    IGetSeasonSession,
    IGetEpisodeSession
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _semaphore;
    
    private const string TitleMetadataCollectionName = "titleMetaData";
    private const string ReviewsCollectionName = "reviews";
    private const string AvatarImageFileStorageName = "avatarImageFiles";
    private const string AvatarImageChunkName = "avatarImageChunks";

    private readonly ResiliencePipeline _resiliencePipeline;

    public CrunchyrollUnitOfWork(PluginConfiguration config)
    {
        _connectionString = config.LocalDatabasePath;
        _semaphore = new SemaphoreSlim(1, 1);

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            var location = typeof(CrunchyrollUnitOfWork).Assembly.Location;
            var dbFilePath = Path.Combine(Path.GetDirectoryName(location)!, "Crunchyroll.db");
            _connectionString = $"Filename={dbFilePath}; Connection=Shared;";
        }
        
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()
            {
                ShouldHandle = new PredicateBuilder().Handle<IOException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1)
            })
            .Build();

    }
    
    public ValueTask AddReviewsForTitleIdAsync(string titleId, IReadOnlyList<ReviewItem> reviews)
    {
        _semaphore.Wait();

        try
        {

            _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var reviewsCollection = db.GetCollection<TitleReviews>(ReviewsCollectionName);

                var entity = new TitleReviews()
                {
                    TitleId = titleId,
                    Reviews = reviews,
                };

                reviewsCollection.Insert(entity);
                reviewsCollection.EnsureIndex(x => x.TitleId, true);
            });
            
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
            var reviewsEntity = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var reviewsCollection = db.GetCollection<TitleReviews>(ReviewsCollectionName);
                return reviewsCollection.FindOne(x => x.TitleId == titleId);
            });

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
            _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);
                var fileStorage = db.GetStorage<string>(AvatarImageFileStorageName, AvatarImageChunkName);
            
                fileStorage.Upload(url, url, imageStream);
            });
            
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
            var exists = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);
                var fileStorage = db.GetStorage<string>(AvatarImageFileStorageName, AvatarImageChunkName);

                return fileStorage.Exists(url);
            });
            
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
            var memoryStream = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);
                var fileStorage = db.GetStorage<string>(AvatarImageFileStorageName, AvatarImageChunkName);

                var fileInfo = fileStorage.FindById(url);
                
                if (fileInfo is null)
                {
                    return null;
                }
            
                var memoryStream = new MemoryStream();
                fileInfo.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                
                return memoryStream;
            });

            if (memoryStream is null)
            {
                return ValueTask.FromResult<Stream?>(null);
            }
            
            return ValueTask.FromResult<Stream?>(memoryStream);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask AddOrUpdateTitleMetadata(TitleMetadata.Entities.TitleMetadata titleMetadata)
    {
        _semaphore.Wait();

        try
        {
            _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var reviewsCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);
            
                reviewsCollection.Upsert(titleMetadata);
                reviewsCollection.EnsureIndex(x => x.TitleId, true);
            });

            return ValueTask.CompletedTask;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<TitleMetadata.Entities.TitleMetadata?> GetTitleMetadata(string titleId)
    {
        _semaphore.Wait();

        try
        {
            var metadata = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var metadataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);
            
                return metadataCollection.FindOne(x => x.TitleId == titleId);
            });

            return ValueTask.FromResult<TitleMetadata.Entities.TitleMetadata?>(metadata);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<string?> GetSeasonIdByNumberAsync(string titleId, int seasonNumber, int duplicateCounter)
    {
        _semaphore.Wait();

        try
        {
            var seasonId = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var metadataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);

                var season = metadataCollection
                    .Query()
                    .Where(x => x.TitleId == titleId)
                    .FirstOrDefault();

                return season?.Seasons
                    .Where(x => x.SeasonNumber == seasonNumber)
                    .Skip(duplicateCounter)
                    .FirstOrDefault()?
                    .Id;
            });

            return ValueTask.FromResult<string?>(seasonId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<string?> GetSeasonIdByNameAsync(string titleId, string name)
    {
        _semaphore.Wait();

        try
        {
            var seasonId = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var metadataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);

                var seasons = metadataCollection
                        .Query()
                        .Where(x => x.TitleId == titleId)
                        .Select(x => x.Seasons)
                        .FirstOrDefault();
                
                return seasons?.Find(x => x.Title.Contains(name))?.Id;
            });

            return ValueTask.FromResult<string?>(seasonId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<string?> GetEpisodeIdAsync(string titleId, string seasonId, string episodeIdentifier)
    {
        _semaphore.Wait();

        try
        {
            var epsiodeId = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var metadataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);

                var seasons = metadataCollection
                    .Query()
                    .Where(x => x.TitleId == titleId)
                    .Select(x => x.Seasons)
                    .FirstOrDefault();
                
                var season = seasons?.Find(x => x.Id == seasonId);

                return season?.Episodes.Find(x => x.EpisodeNumber == episodeIdentifier)?.Id;
            });

            return ValueTask.FromResult<string?>(epsiodeId);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}