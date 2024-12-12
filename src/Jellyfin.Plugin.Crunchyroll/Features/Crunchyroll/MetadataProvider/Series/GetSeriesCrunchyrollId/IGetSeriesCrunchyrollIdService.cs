using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId;

public interface IGetSeriesCrunchyrollIdService
{
    public Task<Result<CrunchyrollId?>> GetSeriesCrunchyrollId(string name, CultureInfo language,
        CancellationToken cancellationToken);
}