using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;

public interface ICrunchyrollSearchClient
{
    /// <param name="query">Search term</param>
    /// <param name="type">Crunchyroll content type, e.g. "series" or "episode"</param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>All found items of the given type</returns>
    public Task<Result<IReadOnlyList<CrunchyrollSearchDataItem>>> SearchAsync(string query, string type,
        CultureInfo language, CancellationToken cancellationToken);
}