using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;

public interface IScrapSeasonMetadataRepository : ISaveChanges
{
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId seriesId, 
        CultureInfo language, CancellationToken cancellationToken);

    public void UpdateTitleMetadata(Domain.Entities.TitleMetadata titleMetadata);
}