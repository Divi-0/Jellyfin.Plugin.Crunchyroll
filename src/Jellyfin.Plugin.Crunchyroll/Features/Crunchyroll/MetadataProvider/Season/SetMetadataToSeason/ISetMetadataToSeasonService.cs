using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;

public interface ISetMetadataToSeasonService
{
    public Task<Result<MediaBrowser.Controller.Entities.TV.Season>> SetMetadataToSeasonAsync(CrunchyrollId seasonId,
        CultureInfo language, int? currentIndexNumber, CancellationToken cancellationToken);
}