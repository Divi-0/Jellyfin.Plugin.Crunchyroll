using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;

public class AddAvatarService : IAddAvatarService
{
    private readonly IAddAvatarSession _session;
    private readonly IAvatarClient _client;

    public AddAvatarService(IAddAvatarSession session, IAvatarClient client)
    {
        _session = session;
        _client = client;
    }
    
    public async ValueTask<Result> AddAvatarIfNotExists(string uri, CancellationToken cancellationToken)
    {
        var avatarResult = await _client.GetAvatarStreamAsync(uri, cancellationToken);

        if (avatarResult.IsFailed)
        {
            return avatarResult.ToResult();
        }

        var imageStream = avatarResult.Value;
            
        var addAvatarResult = await _session.AddAvatarImageAsync(uri, imageStream);
        
        if (addAvatarResult.IsFailed)
        {
            return addAvatarResult;
        }
        
        return Result.Ok();
    }
}