using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Client;

public record CrunchyrollCommentsItemUserAttributesAvatar
{
    public IReadOnlyList<CrunchyrollCommentsItemUserAttributesAvatarIcon> Unlocked { get; init; } =
        new List<CrunchyrollCommentsItemUserAttributesAvatarIcon>();
}