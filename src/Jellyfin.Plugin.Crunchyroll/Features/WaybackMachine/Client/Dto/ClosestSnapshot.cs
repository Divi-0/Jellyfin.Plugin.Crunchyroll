namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;

public record ClosestSnapshot
{
    public bool Available { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}