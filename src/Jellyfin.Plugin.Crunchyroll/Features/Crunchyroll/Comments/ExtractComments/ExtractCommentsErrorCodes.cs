namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public static class ExtractCommentsErrorCodes
{
    public const string TitleAlreadyHasReviews = "1001";
    public const string GetAvatarImageRequestFailed = "1002";
    public const string HtmlUrlRequestFailed = "1003";
    public const string HtmlExtractorInvalidCrunchyrollCommentsPage = "1004";
}