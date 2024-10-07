using AutoFixture;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;

public class CrunchyrollReviewsResponseCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register<CrunchyrollReviewsResponse>(() => Customized(fixture));
    }

    public static CrunchyrollReviewsResponse Customized(IFixture fixture)
    {
        var reviewItem = fixture
            .Build<CrunchyrollReviewItemReview>()
            .With(x => x.CreatedAt, fixture.Create<DateTime>().ToString("yyyy-MM-ddTHH:mm:ssZ"))
            .With(x => x.ModifiedAt, fixture.Create<DateTime>().ToString("yyyy-MM-ddTHH:mm:ssZ"))
            .Create();
        
        var rewiews = fixture
            .Build<CrunchyrollReviewItem>()
            .With(x => x.AuthorRating, "3s")
            .With(x => x.Review, reviewItem)
            .CreateMany();
        
        var response = new CrunchyrollReviewsResponse { Items = rewiews.ToList() };

        return response;
    }
}