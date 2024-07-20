namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;

public record Snapshot
{
    public ClosestSnapshot? Closest { get; init; }
}