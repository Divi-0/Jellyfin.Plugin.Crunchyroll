using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.Entities;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class DatabaseMockHelper
{
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    
    public static IReadOnlyList<ReviewItem> InsertRandomReviews(string dbFilePath, string titleId)
    {
        Semaphore.Wait();

        try
        {
            var fixture = new Fixture();

            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var reviewsCollection = db.GetCollection<TitleReviews>("reviews");

            var reviews = fixture.CreateMany<ReviewItem>().ToList();

            var entity = new TitleReviews
            {
                TitleId = titleId,
                Reviews = reviews
            };

            reviewsCollection.Insert(entity);

            return reviews;
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    public static IReadOnlyList<CommentItem> InsertRandomComments(string dbFilePath, string episodeId)
    {
        Semaphore.Wait();

        try
        {
            var fixture = new Fixture();

            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var commentsCollection = db.GetCollection<EpisodeComments>("comments");

            var comments = fixture.CreateMany<CommentItem>().ToList();

            var entity = new EpisodeComments
            {
                EpisodeId = episodeId,
                Comments = comments
            };

            commentsCollection.Insert(entity);

            return comments;
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    public static void ShouldHaveReviews(string dbFilePath, string titleId)
    {
        Semaphore.Wait();

        try
        {
            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var reviewsCollection = db.GetCollection<TitleReviews>("reviews");
            
            var hasReviews = reviewsCollection.Exists(x => x.TitleId == titleId);

            hasReviews.Should().BeTrue();
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    public static void ShouldHaveComments(string dbFilePath, string episodeId)
    {
        Semaphore.Wait();

        try
        {
            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var commentsCollection = db.GetCollection<EpisodeComments>("comments");
            
            var hasComments = commentsCollection.Exists(x => x.EpisodeId == episodeId);

            hasComments.Should().BeTrue();
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    public static void ShouldHaveAvatarUri(string dbFilePath, string uri)
    {
        Semaphore.Wait();

        try
        {
            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var fileStorage = db.GetStorage<string>("avatarImageFiles", "avatarImageChunks");

            var hasAvatarUri = fileStorage.Exists(uri);
            
            hasAvatarUri.Should().BeTrue();
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    public static void InsertAvatarImage(string dbFilePath, string imageUrl, Stream imageStream)
    {
        Semaphore.Wait();

        try
        {
            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var fileStorage = db.GetStorage<string>("avatarImageFiles", "avatarImageChunks");

            fileStorage.Upload(imageUrl, imageUrl, imageStream);
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    public static void ShouldHaveMetadata(string dbFilePath, string titleId, CrunchyrollSeriesContentItem seriesContentResponse)
    {
        Semaphore.Wait();

        try
        {
            using var db = new LiteDatabase($"Filename={dbFilePath}; Connection=Shared;");
            var metadataCollection = db.GetCollection<TitleMetadata>("titleMetaData");
            
            var metadata = metadataCollection.FindOne(x => x.TitleId == titleId);

            metadata.Should().NotBeNull();
            metadata.TitleId.Should().NotBeEmpty();
            metadata.SlugTitle.Should().Be(seriesContentResponse.SlugTitle);
            metadata.Title.Should().Be(seriesContentResponse.Title);
            metadata.Description.Should().Be(seriesContentResponse.Description);
            metadata.Studio.Should().Be(seriesContentResponse.ContentProvider);
            metadata.PosterTallUri.Should().Be(seriesContentResponse.Images.PosterTall.First().Last().Source);
            metadata.PosterWideUri.Should().Be(seriesContentResponse.Images.PosterWide.First().Last().Source);
            metadata.Seasons.Should().NotBeEmpty();
            metadata.Seasons.Should().AllSatisfy(x =>
            {
                x.Id.Should().NotBeEmpty();
                x.Title.Should().NotBeEmpty();
                x.SlugTitle.Should().NotBeEmpty();
                x.Episodes.Should().NotBeEmpty();
                x.Episodes.Should().AllSatisfy(episode =>
                {
                   episode.Id.Should().NotBeEmpty(); 
                   episode.Title.Should().NotBeEmpty(); 
                   episode.SlugTitle.Should().NotBeEmpty(); 
                   episode.Description.Should().NotBeEmpty(); 
                });
            });
        }
        finally
        {
            Semaphore.Release();
        }
    }
}