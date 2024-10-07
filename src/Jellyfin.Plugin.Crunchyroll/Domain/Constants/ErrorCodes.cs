namespace Jellyfin.Plugin.Crunchyroll.Domain.Constants;

public static class ErrorCodes
{
    public const string CrunchyrollRequestFailed = "1";
    public const string CrunchyrollLoginFailed = "2";
    public const string CrunchyrollSearchFailed = "3";
    public const string CrunchyrollTitleIdNotFound = "4";
    public const string CrunchyrollSessionMissing = "5";
    public const string CrunchyrollSearchContentIncompatible = "6";
    public const string CrunchyrollGetCommentsFailed = "7";
    public const string ItemNotFound = "8";
    public const string ProviderIdNotSet = "9";
}