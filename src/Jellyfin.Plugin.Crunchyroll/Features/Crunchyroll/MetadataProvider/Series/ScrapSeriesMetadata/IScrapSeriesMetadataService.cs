using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;

public interface IScrapSeriesMetadataService
{
    public Task<Result> ScrapSeriesMetadataAsync(CrunchyrollId crunchyrollId, CultureInfo language,
        CancellationToken cancellationToken);
}