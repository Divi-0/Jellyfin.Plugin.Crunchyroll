using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.Entites;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.ExtractComments;

public interface IExtractCommentsSession : IAddAvatarSession
{
    public ValueTask InsertComments(EpisodeComments comments);
    public ValueTask<bool> CommentsForEpisodeExists(string episodeId);
}