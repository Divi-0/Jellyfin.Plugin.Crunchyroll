namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;

public static class ExtractReviewsErrorCodes
{
    public const string TitleAlreadyHasReviews = "301";
    public const string GetAvatarImageRequestFailed = "302";
    public const string HtmlUrlRequestFailed = "303";
    public const string HtmlExtractorInvalidCrunchyrollReviewsPage = "304";
}