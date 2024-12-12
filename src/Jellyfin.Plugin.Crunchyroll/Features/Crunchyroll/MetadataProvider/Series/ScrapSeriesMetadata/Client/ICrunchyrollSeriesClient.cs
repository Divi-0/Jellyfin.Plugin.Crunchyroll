using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata.Client;

public interface ICrunchyrollSeriesClient
{
    public Task<Result<CrunchyrollSeriesContentItem>> GetSeriesMetadataAsync(CrunchyrollId titleId, CultureInfo language, 
        CancellationToken cancellationToken);
    public Task<Result<Stream>> GetPosterImagesAsync(string url, CancellationToken cancellationToken);
    public Task<Result<float>> GetRatingAsync(CrunchyrollId titleId, CancellationToken cancellationToken);
}