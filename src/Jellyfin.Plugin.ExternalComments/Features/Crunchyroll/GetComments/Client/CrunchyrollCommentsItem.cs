using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Client;

public record CrunchyrollCommentsItem
{
    [JsonPropertyName("__href__")]
    public string Href { get; init; } = string.Empty;
    
    [JsonPropertyName("__links__")]
    public object Links { get; init; } = new {};
    
    public required CrunchyrollCommentsItemUser User { get; init; }
    
    public string Message { get; init; }= string.Empty;
    
    public required CrunchyrollCommentsItemVotes Votes { get; init; }
    
    [JsonPropertyName("replies_count")]
    public int RepliesCount { get; init; }
}