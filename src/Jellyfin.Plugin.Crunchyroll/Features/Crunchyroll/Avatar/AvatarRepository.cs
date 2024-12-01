using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.GetAvatar;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;

public class AvatarRepository : IAddAvatarRepository, IGetAvatarRepository
{
    private readonly IDirectory _directory;
    private readonly IFile _file;
    private readonly ILogger<AvatarRepository> _logger;

    private readonly string _directoryPath;

    public AvatarRepository(IDirectory directory, IFile file, ILogger<AvatarRepository> logger)
    {
        _directory = directory;
        _file = file;
        _logger = logger;

        _directoryPath = Path.Combine(
            Path.GetDirectoryName(typeof(AvatarRepository).Assembly.Location)!,
            "avatar-images");

        directory.CreateDirectory(_directoryPath);
    }
    
    public async Task<Result> AddAvatarImageAsync(string fileName, Stream imageStream, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = Path.Combine(_directoryPath, fileName);

            await using var fileStream = _file.Create(filePath);
            await imageStream.CopyToAsync(fileStream, cancellationToken);
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create image for filename {FileName}", fileName);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public Result<bool> AvatarExists(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_directoryPath, fileName);
            return _file.Exists(filePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to check if file exists {FileName}", fileName);
            return Result.Fail(ErrorCodes.Internal);
        }
    }

    public async Task<Result<Stream?>> GetAvatarImageAsync(string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = Path.Combine(_directoryPath, fileName);

            if (!_file.Exists(filePath))
            {
                return Result.Ok<Stream?>(null);
            }
            
            await using var filestream = _file.OpenRead(filePath);
            var memoryStream = new MemoryStream();
            await filestream.CopyToAsync(memoryStream, cancellationToken: cancellationToken);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to check if file exists {FileName}", fileName);
            return Result.Fail(ErrorCodes.Internal);
        }
    }
}