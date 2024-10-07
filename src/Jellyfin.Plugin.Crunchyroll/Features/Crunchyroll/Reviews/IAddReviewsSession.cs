using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews;

public interface IAddReviewsSession : IAddAvatarSession
{
    public ValueTask AddReviewsForTitleIdAsync(string titleId, IReadOnlyList<ReviewItem> reviews);
}