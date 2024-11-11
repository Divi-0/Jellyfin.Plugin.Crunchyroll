using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

public partial class OverwriteEpisodeJellyfinDataTask : IPostEpisodeIdSetTask
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OverwriteEpisodeJellyfinDataTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IOverwriteEpisodeJellyfinDataTaskSession _session;
    private readonly IFile _file;
    private readonly IDirectory _directory;
    
    private readonly string _thumbnailDirPath = Path.Combine(
        Path.GetDirectoryName(typeof(OverwriteEpisodeJellyfinDataTask).Assembly.Location)!, 
        "episode-thumbnails");

    public OverwriteEpisodeJellyfinDataTask(IHttpClientFactory httpClientFactory, ILogger<OverwriteEpisodeJellyfinDataTask> logger,
        ILibraryManager libraryManager, IOverwriteEpisodeJellyfinDataTaskSession session, IFile file, IDirectory directory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _libraryManager = libraryManager;
        _session = session;
        _file = file;
        _directory = directory;
    }
    
    public async Task RunAsync(BaseItem episodeItem, CancellationToken cancellationToken)
    {
        var hasEpisodeId = episodeItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.EpisodeId, 
                               out var episodeId) && !string.IsNullOrWhiteSpace(episodeId);

        if (!hasEpisodeId)
        {
            _logger.LogDebug("Episode with name {Name} has no crunchyroll id. Skipping...", episodeItem.Name);
            return;
        }

        var createDirectoryResult = CreateDirectoryIfNotExists(_thumbnailDirPath);

        if (createDirectoryResult.IsFailed)
        {
            return;
        }

        var crunchyrollEpisodeResult = await _session.GetEpisodeAsync(episodeId!);

        if (crunchyrollEpisodeResult.IsFailed)
        {
            return;
        }

        var crunchyrollEpisode = crunchyrollEpisodeResult.Value;
        if (!string.IsNullOrWhiteSpace(crunchyrollEpisode.ThumbnailUrl))
        {
            var imageStreamResult = await GetThumbnailImageStreamAsync(crunchyrollEpisode.ThumbnailUrl, cancellationToken);

            if (imageStreamResult.IsSuccess)
            {
                var filePath = Path.Combine(_thumbnailDirPath, Path.GetFileName(crunchyrollEpisode.ThumbnailUrl));
                var createImageResult = await CreateFileAsync(filePath, imageStreamResult.Value, cancellationToken);

                if (createImageResult.IsFailed)
                {
                    return;
                }
            
                episodeItem.SetImage(new ItemImageInfo()
                {
                    Path = filePath,
                    Type = ImageType.Thumb,
                }, 0);
            
                episodeItem.SetImage(new ItemImageInfo()
                {
                    Path = filePath,
                    Type = ImageType.Primary,
                }, 0);
            }
        }

        episodeItem.Name = crunchyrollEpisode.Title;
        episodeItem.Overview = crunchyrollEpisode.Description;

        if (!episodeItem.IndexNumber.HasValue)
        {
            var match = EpisodeNumberRegex().Match(crunchyrollEpisode.EpisodeNumber);

            if (match.Success && Math.Abs(double.Parse(match.Value) - crunchyrollEpisode.SequenceNumber) < 0.5)
            {
                episodeItem.IndexNumber = int.Parse(match.Value);
            }
            
            episodeItem.Name = $"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}";
            
            if (crunchyrollEpisode.SequenceNumber % 1 != 0)
            {
                episodeItem.ProviderIds[CrunchyrollExternalKeys.EpisodeDecimalEpisodeNumber] = 
                    crunchyrollEpisode.SequenceNumber.ToString("0.0");
            }
        }
        
        await _libraryManager.UpdateItemAsync(episodeItem, episodeItem.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
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
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get image for url {Url}", url);
                return Result.Fail(ErrorCodes.HttpRequestFailed);
            }
        
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get image for url {Url}", url);
            return Result.Fail(ErrorCodes.HttpRequestFailed);
        }
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex EpisodeNumberRegex();
}