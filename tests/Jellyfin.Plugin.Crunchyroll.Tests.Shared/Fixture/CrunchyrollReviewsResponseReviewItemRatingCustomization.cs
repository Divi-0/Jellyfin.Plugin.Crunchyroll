using AutoFixture;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;

public class CrunchyrollReviewsResponseReviewItemRatingCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register<CrunchyrollReviewItemRatingsItem>(() => Customized(fixture));
    }

    public static CrunchyrollReviewItemRatingsItem Customized(IFixture fixture)
    {
        CrunchyrollReviewItemRatingsItem ratingItem;
        if (Random.Shared.Next(1, 100) % 2 == 0)
        {
            ratingItem = fixture
                .Build<CrunchyrollReviewItemRatingsItem>()
                .With(x => x.Displayed, fixture.Create<int>().ToString)
                .With(x => x.Unit, string.Empty)
                .Create();
        }
        else
        {
            ratingItem = fixture
                .Build<CrunchyrollReviewItemRatingsItem>()
                .With(x => x.Displayed, fixture.Create<double>().ToString("#.#"))
                .With(x => x.Unit, "K")
                .Create();
        }
        
        return ratingItem;
    }
}