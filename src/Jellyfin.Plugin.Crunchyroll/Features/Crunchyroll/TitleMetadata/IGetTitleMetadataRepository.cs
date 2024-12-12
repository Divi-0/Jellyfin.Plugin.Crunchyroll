using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata;

public interface IGetTitleMetadataRepository
{
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(string titleId, CultureInfo language,
        CancellationToken cancellationToken);
}