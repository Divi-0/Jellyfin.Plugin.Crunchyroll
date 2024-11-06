using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Helper;

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
        var archivedUrl = WaybackMachineImageHelper.GetArchivedImageUri(uri);
        
        var existsResult = await _session.AvatarExistsAsync(archivedUrl);
        
        if (existsResult.IsFailed)
        {
            return existsResult.ToResult();
        }

        if (existsResult.Value)
        {
            //Avatar exists already, no need to store it again
            return Result.Ok();
        }
        
        var avatarResult = await _client.GetAvatarStreamAsync(uri, cancellationToken);

        if (avatarResult.IsFailed)
        {
            return avatarResult.ToResult();
        }

        var imageStream = avatarResult.Value;
            
        var addAvatarResult = await _session.AddAvatarImageAsync(archivedUrl, imageStream);
        
        return addAvatarResult.IsSuccess 
            ? Result.Ok() 
            : addAvatarResult;
    }
}