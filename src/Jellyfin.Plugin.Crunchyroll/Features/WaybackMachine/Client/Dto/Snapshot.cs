namespace Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;

public record Snapshot
{
    public ClosestSnapshot? Closest { get; init; }
}