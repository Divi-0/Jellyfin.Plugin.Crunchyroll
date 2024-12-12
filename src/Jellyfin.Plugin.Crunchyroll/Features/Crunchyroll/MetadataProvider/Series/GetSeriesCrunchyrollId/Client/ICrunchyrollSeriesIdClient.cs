using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId.Client;

public interface ICrunchyrollSeriesIdClient
{
    /// <param name="title">Title of the series/movie</param>
    /// <param name="language"></param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>TitleId of the matching series</returns>
    public Task<Result<CrunchyrollId?>> GetSeriesIdAsync(string title, CultureInfo language, 
        CancellationToken cancellationToken);
}