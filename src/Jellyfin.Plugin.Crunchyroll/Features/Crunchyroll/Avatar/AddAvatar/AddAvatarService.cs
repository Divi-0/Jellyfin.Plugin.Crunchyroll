using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Helper;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;

public class AddAvatarService : IAddAvatarService
{
    private readonly IAddAvatarRepository _repository;
    private readonly IAvatarClient _client;
    
    private readonly ConcurrentDictionary<string, bool> _concurrentDictionary = new();

    public AddAvatarService(IAddAvatarRepository repository, IAvatarClient client)
    {
        _repository = repository;
        _client = client;
    }
    
    public async ValueTask<Result<string>> AddAvatarIfNotExists(string uri, CancellationToken cancellationToken)
    {
        var archivedUrl = WaybackMachineImageHelper.GetArchivedImageUri(uri);
        
        if (!_concurrentDictionary.TryAdd(uri, true))
        {
            return Result.Ok(archivedUrl);
        }

        var fileName = Path.GetFileName(archivedUrl).Replace("jpe", "jpeg");//TODO: Test jpe jpeg
        var existsResult = _repository.AvatarExists(fileName);
        
        if (existsResult.IsFailed)
        {
            return existsResult.ToResult();
        }

        if (existsResult.Value)
        {
            //Avatar exists already, no need to store it again
            return Result.Ok(archivedUrl);
        }
        
        var avatarResult = await _client.GetAvatarStreamAsync(uri, cancellationToken);

        if (avatarResult.IsFailed)
        {
            return avatarResult.ToResult();
        }

        var imageStream = avatarResult.Value;
            
        var addAvatarResult = await _repository.AddAvatarImageAsync(fileName, imageStream, cancellationToken);

        _concurrentDictionary.TryRemove(uri, out _);
        
        return addAvatarResult.IsSuccess 
            ? Result.Ok(archivedUrl) 
            : addAvatarResult;
    }
}