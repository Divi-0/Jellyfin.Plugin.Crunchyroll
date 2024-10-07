using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;

public interface IExtractCommentsSession : IAddAvatarSession
{
    public ValueTask InsertComments(EpisodeComments comments);
    public ValueTask<bool> CommentsForEpisodeExists(string episodeId);
}