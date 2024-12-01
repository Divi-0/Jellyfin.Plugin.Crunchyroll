using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;

public interface IOverwriteSeasonJellyfinDataRepository
{
    public Task<Result<Season?>> GetSeasonAsync(string crunchyrollSeasonId, CultureInfo language,
        CancellationToken cancellationToken);
}