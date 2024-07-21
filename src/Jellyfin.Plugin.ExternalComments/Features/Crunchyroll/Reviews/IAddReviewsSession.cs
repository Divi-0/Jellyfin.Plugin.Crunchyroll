using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;

public interface IAddReviewsSession : IAddAvatarSession
{
    public ValueTask AddReviewsForTitleIdAsync(string titleId, IReadOnlyList<ReviewItem> reviews);
}