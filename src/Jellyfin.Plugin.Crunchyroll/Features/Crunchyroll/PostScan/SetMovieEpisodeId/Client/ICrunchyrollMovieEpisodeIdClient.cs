using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId.Client;

public interface ICrunchyrollMovieEpisodeIdClient
{
    public Task<Result<SearchResponse?>> SearchTitleIdAsync(string name, CancellationToken cancellationToken);
}