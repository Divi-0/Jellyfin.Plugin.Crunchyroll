using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;

public record AvatarQuery : IRequest<Result<Stream?>>
{
    public required string Url { get; init; }
}

public sealed class AvatarQueryHandler : IRequestHandler<AvatarQuery, Result<Stream?>>
{
    private readonly IGetAvatarRepository _repository;

    public AvatarQueryHandler(IGetAvatarRepository repository)
    {
        _repository = repository;
    }
    
    public async ValueTask<Result<Stream?>> Handle(AvatarQuery request, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(request.Url).Replace("jpe", "jpeg");
        return await _repository.GetAvatarImageAsync(fileName, cancellationToken);
    }
}