using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public interface IScrapEpisodeMetadataRepository : ISaveChanges
{
    public Task<Result<Domain.Entities.Season?>> GetSeasonAsync(CrunchyrollId seasonId, CultureInfo language,
        CancellationToken cancellationToken);

    public void UpdateSeason(Domain.Entities.Season season);
}