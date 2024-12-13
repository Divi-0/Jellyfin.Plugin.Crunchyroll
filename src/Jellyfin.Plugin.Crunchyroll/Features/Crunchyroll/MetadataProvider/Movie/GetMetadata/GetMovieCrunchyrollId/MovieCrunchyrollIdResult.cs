using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;

public record MovieCrunchyrollIdResult
{
    public required CrunchyrollId SeriesId { get; init; }
    public required CrunchyrollId SeasonId { get; init; }
    public required CrunchyrollId EpisodeId { get; init; }
}