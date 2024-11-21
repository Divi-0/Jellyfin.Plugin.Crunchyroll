using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId.Client;

public interface ICrunchyrollTitleIdClient
{
    /// <param name="title">Title of the series/movie</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>TitleId of the matching series</returns>
    public Task<Result<SearchResponse?>> GetTitleIdAsync(string title, CancellationToken cancellationToken);
}