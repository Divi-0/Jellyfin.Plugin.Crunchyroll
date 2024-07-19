using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;

public record AvailabilityResponse
{
    [JsonPropertyName("archived_snapshots")]
    public required Snapshot ArchivedSnapshots { get; init; }
}