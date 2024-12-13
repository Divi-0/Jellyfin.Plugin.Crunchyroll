using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;

public interface IScrapSeriesMetadataRepository : ISaveChanges
{
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId titleId, CultureInfo language,
        CancellationToken cancellationToken);
    
    public Task<Result> AddOrUpdateTitleMetadata(Domain.Entities.TitleMetadata titleMetadata, 
        CancellationToken cancellationToken);
}