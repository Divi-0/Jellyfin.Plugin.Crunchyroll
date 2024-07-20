using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class DatabaseMockHelper
{
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    
    public static IReadOnlyList<ReviewItem> InsertMockData(string dbFilePath, string titleId)
    {
        Semaphore.Wait();

        try
        {
            var fixture = new Fixture();

            using var db = new LiteDatabase(dbFilePath);
            var reviewsCollection = db.GetCollection<TitleReviews>("reviews");

            var reviews = fixture.CreateMany<ReviewItem>().ToList();

            var entity = new TitleReviews()
            {
                TitleId = titleId,
                Reviews = reviews,
            };

            reviewsCollection.Insert(entity);

            return reviews;
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
            using var db = new LiteDatabase(dbFilePath);
            var reviewsCollection = db.GetCollection<TitleReviews>("reviews");
            
            var hasReviews = reviewsCollection.Exists(x => x.TitleId == titleId);

            hasReviews.Should().BeTrue();
        }
        finally
        {
            Semaphore.Release();
        }
    }
}