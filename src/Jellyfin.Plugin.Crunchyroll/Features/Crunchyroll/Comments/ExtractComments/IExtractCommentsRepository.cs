using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common.Persistence;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public interface IExtractCommentsRepository : ISaveChanges
{
    public Task<Result> AddCommentsAsync(EpisodeComments comments, CancellationToken cancellationToken);
    public Task<Result<bool>> CommentsForEpisodeExistsAsync(string crunchyrollEpisodeId, CancellationToken cancellationToken);
    public Task<Result<string?>> GetEpisodeSlugTitleAsync(CrunchyrollId episodeId, CancellationToken cancellationToken);
}