namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;

public record Snapshot
{
    public required ClosestSnapshot Closest { get; init; }
}