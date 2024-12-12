using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;

public interface ISetMetadataToSeasonRepository
{
    public Task<Result<Domain.Entities.Season?>> GetSeasonAsync(CrunchyrollId seasonId,
        CultureInfo language, CancellationToken cancellationToken);
}