using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public interface IScrapTitleMetadataRepository : IGetTitleMetadataRepository, ISaveChanges
{
    public Task<Result> AddOrUpdateTitleMetadata(Entities.TitleMetadata titleMetadata, 
        CancellationToken cancellationToken);
}