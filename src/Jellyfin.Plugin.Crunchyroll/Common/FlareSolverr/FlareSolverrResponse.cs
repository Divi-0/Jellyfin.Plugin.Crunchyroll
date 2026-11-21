namespace Jellyfin.Plugin.Crunchyroll.Common.FlareSolverr;

public sealed record FlareSolverrResponse()
{
    public required string Status { get; init; }
    public string? Message { get; init; }
    public FlareSolverrResponseSolution? Solution { get; init; }
}

public sealed record FlareSolverrResponseSolution()
{
    public required string Response { get; init; }
}