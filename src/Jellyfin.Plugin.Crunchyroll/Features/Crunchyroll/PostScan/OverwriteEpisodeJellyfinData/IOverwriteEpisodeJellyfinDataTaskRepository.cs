using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

public interface IOverwriteEpisodeJellyfinDataTaskRepository
{
    public Task<Result<Episode?>> GetEpisodeAsync(string crunchyrollEpisodeId, CultureInfo language,
        CancellationToken cancellationToken);
}