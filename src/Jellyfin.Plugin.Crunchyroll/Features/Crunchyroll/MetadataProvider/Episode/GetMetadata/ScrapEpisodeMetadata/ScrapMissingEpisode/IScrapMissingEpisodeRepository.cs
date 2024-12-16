using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;

public interface IScrapMissingEpisodeRepository : ISaveChanges
{
    public Task<Result<bool>> EpisodeExistsAsync(CrunchyrollId episodeId, CultureInfo language,
        CancellationToken cancellationToken);
    
    public Task<Result<Domain.Entities.TitleMetadata?>> GetTitleMetadataAsync(CrunchyrollId titleId, CultureInfo language,
        CancellationToken cancellationToken);
    
    public Task<Result> AddOrUpdateTitleMetadataAsync(Domain.Entities.TitleMetadata titleMetadata, 
        CancellationToken cancellationToken);
}