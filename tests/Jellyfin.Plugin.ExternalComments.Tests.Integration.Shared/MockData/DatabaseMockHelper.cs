using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.Entities;
using LiteDB;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared.MockData;

public static class DatabaseMockHelper
{
    public static IReadOnlyList<ReviewItem> InsertMockData(string dbFilePath, string titleId)
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
}