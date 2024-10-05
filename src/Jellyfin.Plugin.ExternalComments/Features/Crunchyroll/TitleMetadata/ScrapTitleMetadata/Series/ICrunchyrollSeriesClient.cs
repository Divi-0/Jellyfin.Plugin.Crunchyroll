using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;

public interface ICrunchyrollSeriesClient
{
    public Task<Result<CrunchyrollSeriesContentResponse>> GetSeriesMetadataAsync(string titleId, CancellationToken cancellationToken);
    public Task<Result<Stream>> GetPosterImagesAsync(CrunchyrollSeriesImage image, CancellationToken cancellationToken);
}