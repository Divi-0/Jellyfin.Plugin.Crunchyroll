using System.Linq;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search;

public static class CrunchyrollSearchDataImageExtensions
{
    /// <returns>Source of the largest image, or null if there is none</returns>
    public static string? GetLargestImageSource(this CrunchyrollSearchDataImage[][]? images)
    {
        return images?
            .SelectMany(x => x)
            .MaxBy(x => x.Width)?
            .Source;
    }
}