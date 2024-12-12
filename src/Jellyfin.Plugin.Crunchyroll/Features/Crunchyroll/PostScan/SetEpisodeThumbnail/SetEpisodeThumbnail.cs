using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;

public class SetEpisodeThumbnail : ISetEpisodeThumbnail
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFile _file;
    private readonly IDirectory _directory;
    private readonly ILogger<SetEpisodeThumbnail> _logger;
    
    private readonly string _thumbnailDirPath = Path.Combine(
        Path.GetDirectoryName(typeof(SetEpisodeThumbnail).Assembly.Location)!, 
        "episode-thumbnails");

    private const string HttpClientName = "ThumbnailClient";

    public SetEpisodeThumbnail(IHttpClientFactory httpClientFactory, IFile file, IDirectory directory, 
        ILogger<SetEpisodeThumbnail> logger)
    {
        _httpClientFactory = httpClientFactory;
        _file = file;
        _directory = directory;
        _logger = logger;
    }
    
    public async Task<Result> GetAndSetThumbnailAsync(Episode episode, ImageSource imageSource, CancellationToken cancellationToken)
    {
        return await GetAndSetThumbnailAsync(video: episode, imageSource, cancellationToken);
    }

    public async Task<Result> GetAndSetThumbnailAsync(Movie movie, ImageSource imageSource, CancellationToken cancellationToken)
    {
        return await GetAndSetThumbnailAsync(video: movie, imageSource, cancellationToken);
    }
    
    private async Task<Result> GetAndSetThumbnailAsync(Video video, ImageSource imageSource, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageSource.Uri))
        {
            return Result.Ok();
        }

        var createDirectoryResult = CreateDirectoryIfNotExists(_thumbnailDirPath);

        if (createDirectoryResult.IsFailed)
        {
            return createDirectoryResult;
        }
        
        var filePath = Path.Combine(_thumbnailDirPath, Path.GetFileName(imageSource.Uri))
            .Replace(".jpe", ".jpeg");
        
        var currentThumbImage = video.GetImageInfo(ImageType.Thumb, imageIndex: 0);
        
        if (IsImageEqualToCurrentThumbnail(currentThumbImage, filePath, ImageType.Thumb, imageSource) &&
            _file.Exists(filePath))
        {
            _logger.LogDebug("Image with type {Type} for item with Name {Name} already exists, skipping...", 
                ImageType.Thumb,
                video.FileNameWithoutExtension);
            
            //Overwrite "Primary" image, because jellyfin overwrites index 0 everytime, during normal scan
            video.SetImage(new ItemImageInfo()
            {
                Path = currentThumbImage.Path,
                Type = ImageType.Primary,
                Width = imageSource.Width,
                Height = imageSource.Height
            }, 0);
            
            return Result.Ok();
        }

        if (!_file.Exists(filePath))
        {
            var imageStreamResult = await GetThumbnailImageStreamAsync(imageSource.Uri, cancellationToken);

            if (imageStreamResult.IsFailed)
            {
                return imageStreamResult.ToResult();
            }
        
            var createImageResult = await CreateFileAsync(filePath, imageStreamResult.Value, cancellationToken);

            if (createImageResult.IsFailed)
            {
                return Result.Fail(Domain.Constants.ErrorCodes.Internal);
            }
        }
            
        video.SetImage(new ItemImageInfo()
        {
            Path = filePath,
            Type = ImageType.Thumb,
            Width = imageSource.Width,
            Height = imageSource.Height
        }, 0);
            
        video.SetImage(new ItemImageInfo()
        {
            Path = filePath,
            Type = ImageType.Primary,
            Width = imageSource.Width,
            Height = imageSource.Height
        }, 0);
        
        return Result.Ok();
    }

    private static bool IsImageEqualToCurrentThumbnail(ItemImageInfo? imageInfo, string path, ImageType imageType, 
        ImageSource imageSource)
    {
        return imageInfo is not null &&
               imageInfo.Path == path &&
               imageInfo.Type == imageType &&
               imageInfo.Width == imageSource.Width &&
               imageInfo.Height == imageSource.Height;
    }

    private Result CreateDirectoryIfNotExists(string directoryPath)
    {
        try
        {
            if (!_directory.Exists(_thumbnailDirPath))
            {
                _directory.CreateDirectory(_thumbnailDirPath);
            }

            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create directory {Path}", directoryPath);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }

    private async Task<Result> CreateFileAsync(string filePath, Stream imageStream, CancellationToken cancellationToken)
    {
        try
        {
            await using var fileStream = _file.Create(filePath);
            await imageStream.CopyToAsync(fileStream, cancellationToken);
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create file with filePath {Path}", filePath);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }

    private async Task<Result<Stream>> GetThumbnailImageStreamAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get image for url {Url} with statuscode {StatusCode}", url, response.StatusCode);
                return Result.Fail(Domain.Constants.ErrorCodes.Internal);
            }
        
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get image for url {Url}", url);
            return Result.Fail(Domain.Constants.ErrorCodes.Internal);
        }
    }
}