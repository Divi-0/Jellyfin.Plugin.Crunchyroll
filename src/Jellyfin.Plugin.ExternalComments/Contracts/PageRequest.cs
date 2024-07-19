namespace Jellyfin.Plugin.ExternalComments.Contracts;

public record PageRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}