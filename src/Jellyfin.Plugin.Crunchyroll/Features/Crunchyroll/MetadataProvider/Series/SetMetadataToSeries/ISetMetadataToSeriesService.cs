using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SetMetadataToSeries;

public interface ISetMetadataToSeriesService
{
    public Task<Result<MediaBrowser.Controller.Entities.TV.Series>> SetSeriesMetadataAsync(CrunchyrollId crunchyrollId, 
        CultureInfo language, CancellationToken cancellationToken);
}