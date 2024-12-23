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
    public const string Internal = "10";
    public const string NotAllowed = "11";
    public const string FeatureDisabled = "12";
    public const string NotFound = "13";
}