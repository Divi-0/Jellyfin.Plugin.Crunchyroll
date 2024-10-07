using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;

public record CrunchyrollAuthResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }
    
    [JsonPropertyName("scope")]
    public required string Scope { get; init; }
    
    [JsonPropertyName("country")]
    public required string Country { get; init; }
}