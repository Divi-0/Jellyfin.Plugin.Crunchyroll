using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;

public record CrunchyrollCommentsItemUserAttributesAvatar
{
    public IReadOnlyList<CrunchyrollCommentsItemUserAttributesAvatarIcon> Unlocked { get; init; } =
        new List<CrunchyrollCommentsItemUserAttributesAvatarIcon>();
}