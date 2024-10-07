using System;
using System.Linq;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Mappings;

public static class CrunchyrollReviewResponseMappings
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If author rating is not recognized</exception>
    public static ReviewsResponse ToReviewsResponse(this CrunchyrollReviewsResponse crunchyrollReviewsResponse, string crunchyrollAvatarEndpoint)
    {
        var reviews = crunchyrollReviewsResponse.Items.Select(x => new ReviewItem()
        {
            Author = new ReviewItemAuthor()
            {
                Username = x.Author.Username,
                AvatarUri = $"{crunchyrollAvatarEndpoint}/{x.Author.Avatar}"
            },
            AuthorRating = CrunchyrollAuthorRatingToAuthorRating(x.AuthorRating),
            Title = x.Review.Title,
            Body = x.Review.Body,
            Rating = new ReviewItemRating()
            {
                Likes = ConvertDisplayedToNumber(x.Ratings.Yes),
                Dislikes = ConvertDisplayedToNumber(x.Ratings.No),
                Total = ConvertDisplayedToNumber(x.Ratings.Yes) + ConvertDisplayedToNumber(x.Ratings.No),
            },
            CreatedAt = DateTime.Parse(x.Review.CreatedAt)
        });

        return new ReviewsResponse()
        {
            Reviews = reviews.ToList()
        };
    }

    private static int CrunchyrollAuthorRatingToAuthorRating(string rating)
    {
        return rating switch
        {
            CrunchyrollReviewItemRatingStar.One => 1,
            CrunchyrollReviewItemRatingStar.Two => 2,
            CrunchyrollReviewItemRatingStar.Three => 3,
            CrunchyrollReviewItemRatingStar.Four => 4,
            CrunchyrollReviewItemRatingStar.Five => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(rating), "unknown crunchyroll rating")
        };
    }

    private static int ConvertDisplayedToNumber(CrunchyrollReviewItemRatingsItem item)
    {
        var multiplier = item.Unit switch
        {
            "K" => 1000,
            "k" => 1000,
            "" => 1,
            _ => -1
        };

        return Convert.ToInt32(Math.Round(Convert.ToDouble(item.Displayed) * multiplier));
    }
}