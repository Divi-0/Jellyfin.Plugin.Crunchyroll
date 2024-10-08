using System;

namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;

public record SearchResponse
{
    public required DateTime Timestamp { get; init; }
    public required string MimeType { get; init; }
    public required string Status { get; init; }
}