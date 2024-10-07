using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;

public interface ICrunchyrollSeriesClient
{
    public Task<Result<CrunchyrollSeriesContentItem>> GetSeriesMetadataAsync(string titleId, CancellationToken cancellationToken);
    public Task<Result<Stream>> GetPosterImagesAsync(string url, CancellationToken cancellationToken);
}