namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;

public static class GetReviewsErrorCodes
{
    public const string MappingFailed = "100";
    public const string InvalidResponse = "101";
    public const string RequestFailed = "102";
    public const string NoSession = "103";
    public const string ItemNotFound = "104";
    public const string ItemHasNoProviderId = "105";
}