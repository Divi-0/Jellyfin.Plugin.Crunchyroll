using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;

public interface IGetSeasonCrunchyrollIdService
{
    public Task<Result<CrunchyrollId?>> GetSeasonCrunchyrollId(CrunchyrollId seriesId, string seasonName, int? indexNumber, CultureInfo language,
        CancellationToken cancellationToken);
}