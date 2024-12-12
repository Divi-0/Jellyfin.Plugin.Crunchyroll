using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;

public interface IGetSeriesImageInfosRepository
{
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId seriesId,
        CancellationToken cancellationToken);
}