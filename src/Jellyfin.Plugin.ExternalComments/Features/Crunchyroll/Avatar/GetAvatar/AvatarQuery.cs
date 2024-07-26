using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar.GetAvatar;

public record AvatarQuery : IRequest<Result<Stream?>>
{
    public required string Url { get; init; }
}

public sealed class AvatarQueryHandler : IRequestHandler<AvatarQuery, Result<Stream?>>
{
    private readonly IGetAvatarSession _getAvatarSession;

    public AvatarQueryHandler(IGetAvatarSession getAvatarSession)
    {
        _getAvatarSession = getAvatarSession;
    }
    
    public async ValueTask<Result<Stream?>> Handle(AvatarQuery request, CancellationToken cancellationToken)
    {
        return await _getAvatarSession.GetAvatarImageAsync(request.Url);
    }
}