using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;

public interface IScrapMovieMetadataRepository : ISaveChanges
{
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId titleId, CultureInfo language,
        CancellationToken cancellationToken);
    
    public Task<Result> AddOrUpdateTitleMetadataAsync(Domain.Entities.TitleMetadata titleMetadata, 
        CancellationToken cancellationToken);
}