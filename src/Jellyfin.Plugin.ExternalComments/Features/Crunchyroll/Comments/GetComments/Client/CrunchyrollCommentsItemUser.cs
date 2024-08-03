using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments.Client;

public record CrunchyrollCommentsItemUser
{
    [JsonPropertyName("user_attributes")]
    public required CrunchyrollCommentsItemUserAttributes Attributes { get; init; }
}