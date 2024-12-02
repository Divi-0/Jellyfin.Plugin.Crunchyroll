using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Image.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeriesJellyfinData;

public class OverwriteSeriesJellyfinDataTask : IPostTitleIdSetTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IGetTitleMetadataRepository _repository;
    private readonly IFile _file;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly ILogger<OverwriteSeriesJellyfinDataTask> _logger;
    private readonly IDirectory _directory;

    public OverwriteSeriesJellyfinDataTask(ILibraryManager libraryManager, IGetTitleMetadataRepository repository,
        IFile file, ICrunchyrollSeriesClient crunchyrollSeriesClient, ILogger<OverwriteSeriesJellyfinDataTask> logger,
        IDirectory directory)
    {
        _libraryManager = libraryManager;
        _repository = repository;
        _file = file;
        _crunchyrollSeriesClient = crunchyrollSeriesClient;
        _logger = logger;
        _directory = directory;
    }
    
    public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
    {
        var hasTitleId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var titleId) &&
                    !string.IsNullOrWhiteSpace(titleId);
        
        if (!hasTitleId)
        {
            return;
        }
        
        var titleMetadataResult = await _repository.GetTitleMetadataAsync(titleId!,
            seriesItem.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (titleMetadataResult.IsFailed)
        {
            _logger.LogError("No TitleMetada found for titleId {TitleId}. Skipping...", titleId);
            return;
        }

        var titleMetadata = titleMetadataResult.Value;

        if (titleMetadata is null)
        {
            _logger.LogDebug("No TitleMetadata for series with titleId {TitleId} found. Skipping...", titleId);
            return;
        }

        var posterTall = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterTall)!;
        var posterWide = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterWide)!;

        _ = await GetAndAddImage(seriesItem, posterTall, ImageType.Primary, cancellationToken);
        _ = await GetAndAddImage(seriesItem, posterWide, ImageType.Backdrop, cancellationToken);
        
        seriesItem.Name = titleMetadata.Title;
        seriesItem.Overview = titleMetadata.Description;
        seriesItem.SetStudios([titleMetadata.Studio]);

        await _libraryManager
            .UpdateItemAsync(seriesItem, seriesItem.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private async Task<Result> GetAndAddImage(BaseItem series, ImageSource imageSource, ImageType imageType, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(
            Path.GetDirectoryName(typeof(OverwriteSeriesJellyfinDataTask).Assembly.Location)!,
            "series-images");
        
        var filePath = Path.Combine(directory, Path.GetFileName(imageSource.Uri))
            .Replace(".jpe", ".jpeg");
        
        var currentImage = series.GetImageInfo(imageType, imageIndex: 0);
        
        if (IsImageEqualToCurrent(currentImage, filePath, imageType, imageSource) && 
            _file.Exists(filePath))
        {
            _logger.LogDebug("Image with type {Type} for item with Name {Name} already exists, skipping...", 
                imageType,
                series.Name);
            return Result.Ok();
        }

        if (!_file.Exists(filePath))
        {
            var donwloadImageResult = await DonwloadImageAndStoreToFileSystem(imageSource, directory, filePath,
                cancellationToken);

            if (donwloadImageResult.IsFailed)
            {
                return donwloadImageResult;
            }
        }

        series.SetImage(new ItemImageInfo()
        {
            Path = filePath,
            Type = imageType,
            Width = imageSource.Width,
            Height = imageSource.Height
        }, 0);
        
        return Result.Ok();
    }
    
    private static bool IsImageEqualToCurrent(ItemImageInfo? imageInfo, string path, ImageType imageType, 
        ImageSource imageSource)
    {
        return imageInfo is not null &&
               imageInfo.Path == path &&
               imageInfo.Type == imageType &&
               imageInfo.Width == imageSource.Width &&
               imageInfo.Height == imageSource.Height;
    }

    private async Task<Result> DonwloadImageAndStoreToFileSystem(ImageSource imageSource, string directoryPath,
        string filePath, CancellationToken cancellationToken)
    {
        var posterImageResult = await _crunchyrollSeriesClient.GetPosterImagesAsync(imageSource.Uri, cancellationToken);

        if (posterImageResult.IsFailed)
        {
            return posterImageResult.ToResult();
        }
        
        try
        {
            if (!_directory.Exists(directoryPath))
            {
                _directory.CreateDirectory(directoryPath);
            }
            
            await using var fileStream = _file.Create(filePath);
            await posterImageResult.Value.CopyToAsync(fileStream, cancellationToken);
            
            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error, while creating poster image on filesystem");
            return Result.Fail("FileSystem error");
        }
    }
}