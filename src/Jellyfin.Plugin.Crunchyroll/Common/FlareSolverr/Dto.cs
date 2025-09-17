using System;

namespace Jellyfin.Plugin.Crunchyroll.Common.FlareSolverr;

public record Dto
{
    public required string Cmd { get; init; }
    public required Uri Url { get; init; }
    public int MaxTimeout { get; init; } = 60000;
    public required DtoProxy Proxy { get; init; }
}

public sealed record DtoProxy(Uri Url);

public record PostDto : Dto
{
    public required string PostData { get; init; }
}