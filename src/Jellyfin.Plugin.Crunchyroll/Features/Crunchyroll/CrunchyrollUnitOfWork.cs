using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using LiteDB;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

public sealed class CrunchyrollUnitOfWork : 
    IAddReviewsSession, 
    IGetReviewsSession, 
    IGetAvatarSession,
    IScrapTitleMetadataSession,
    IGetSeasonSession,
    IGetEpisodeSession,
    IExtractCommentsSession,
    IGetCommentsSession,
    IOverwriteEpisodeJellyfinDataTaskSession,
    IOverwriteSeasonJellyfinDataSession
{
    private readonly ILogger<CrunchyrollUnitOfWork> _logger;
    private readonly string _connectionString;
    private readonly SemaphoreSlim _semaphore;
    
    private const string TitleMetadataCollectionName = "titleMetaData";
    private const string ReviewsCollectionName = "reviews";
    private const string AvatarImageFileStorageName = "avatarImageFiles";
    private const string AvatarImageChunkName = "avatarImageChunks";
    private const string CommentsCollectionName = "comments";

    private readonly ResiliencePipeline _resiliencePipeline;

    public CrunchyrollUnitOfWork(PluginConfiguration config, ILogger<CrunchyrollUnitOfWork> logger)
    {
        _logger = logger;
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
                Delay = TimeSpan.FromSeconds(1),
                OnRetry = arg =>
                {
                    _logger.LogDebug("Retrying to access db");
                    return ValueTask.CompletedTask;
                }
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

    public ValueTask<bool> AvatarExistsAsync(string url)
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

    public ValueTask<TitleMetadata.Entities.TitleMetadata?> GetTitleMetadataAsync(string titleId)
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

            return ValueTask.FromResult(seasonId);
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

                var titleMetadata = metadataCollection
                    .Query()
                    .Where(x => x.TitleId == titleId)
                    .FirstOrDefault();
                
                return titleMetadata?.Seasons.FirstOrDefault(x => x.Title.Contains(name))?.Id;
            });

            return ValueTask.FromResult(seasonId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<EpisodeIdResult?> GetEpisodeIdAsync(string titleId, string seasonId, string episodeIdentifier)
    {
        _semaphore.Wait();

        try
        {
            var episode = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var metadataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);

                var titleMetadata = metadataCollection
                    .Query()
                    .Where(x => x.TitleId == titleId)
                    .FirstOrDefault();
                
                var season = titleMetadata?.Seasons.FirstOrDefault(x => x.Id == seasonId);

                return season?.Episodes.FirstOrDefault(x => x.EpisodeNumber == episodeIdentifier);
            });

            return ValueTask.FromResult(episode is null ? 
                null : 
                new EpisodeIdResult(episode.Id, episode.SlugTitle));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask InsertComments(EpisodeComments comments)
    {
        _semaphore.Wait();

        try
        {
            _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var commentsCollection = db.GetCollection<EpisodeComments>(CommentsCollectionName);

                commentsCollection.Insert(comments);
                commentsCollection.EnsureIndex(x => x.EpisodeId, true);
            });

            return ValueTask.CompletedTask;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<bool> CommentsForEpisodeExists(string episodeId)
    {
        _semaphore.Wait();

        try
        {
            var exists = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var commentsCollection = db.GetCollection<EpisodeComments>(CommentsCollectionName);

                return commentsCollection.Exists(x => x.EpisodeId == episodeId);
            });

            return ValueTask.FromResult(exists);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<IReadOnlyList<CommentItem>> GetCommentsAsync(string episodeId, int pageSize, int pageNumber)
    {
        _semaphore.Wait();

        try
        {
            var comments = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var commentsCollection = db.GetCollection<EpisodeComments>(CommentsCollectionName);

                var comments = commentsCollection
                    .Query()
                    .Where(x => x.EpisodeId == episodeId)
                    .FirstOrDefault();

                return comments?.Comments.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToList() ?? [];
            });

            return ValueTask.FromResult<IReadOnlyList<CommentItem>>(comments);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<Result<Episode>> GetEpisodeAsync(string episodeId)
    {
        _semaphore.Wait();

        try
        {
            var episode = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var titleMetaDataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);

                var episode = titleMetaDataCollection
                    .FindAll()
                    .SelectMany(x => x.Seasons)
                    .SelectMany(x => x.Episodes)
                    .FirstOrDefault(x => x.Id == episodeId);

                return episode;
            });

            return episode is null 
                ? ValueTask.FromResult<Result<Episode>>(Result.Fail(Domain.Constants.ErrorCodes.ItemNotFound)) 
                : ValueTask.FromResult<Result<Episode>>(episode);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get episode with episodeId {EpisodeId}", episodeId);
            return ValueTask.FromResult<Result<Episode>>(Result.Fail(Domain.Constants.ErrorCodes.Internal));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<Result<Season>> GetSeasonAsync(string seasonId)
    {
        _semaphore.Wait();

        try
        {
            var season = _resiliencePipeline.Execute(() =>
            {
                using var db = new LiteDatabase(_connectionString);

                var titleMetaDataCollection = db.GetCollection<TitleMetadata.Entities.TitleMetadata>(TitleMetadataCollectionName);

                var episode = titleMetaDataCollection
                    .FindAll()
                    .SelectMany(x => x.Seasons)
                    .FirstOrDefault(x => x.Id == seasonId);

                return episode;
            });

            return season is null 
                ? ValueTask.FromResult<Result<Season>>(Result.Fail(Domain.Constants.ErrorCodes.ItemNotFound)) 
                : ValueTask.FromResult<Result<Season>>(season);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get season with seasonId {SeasonId}", seasonId);
            return ValueTask.FromResult<Result<Season>>(Result.Fail(Domain.Constants.ErrorCodes.Internal));
        }
        finally
        {
            _semaphore.Release();
        }
    }
}