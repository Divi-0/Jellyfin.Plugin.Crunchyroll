using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata;
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
    private readonly PluginConfiguration _config;

    public OverwriteSeriesJellyfinDataTask(ILibraryManager libraryManager, IGetTitleMetadataRepository repository,
        IFile file, ICrunchyrollSeriesClient crunchyrollSeriesClient, ILogger<OverwriteSeriesJellyfinDataTask> logger,
        IDirectory directory, PluginConfiguration config)
    {
        _libraryManager = libraryManager;
        _repository = repository;
        _file = file;
        _crunchyrollSeriesClient = crunchyrollSeriesClient;
        _logger = logger;
        _directory = directory;
        _config = config;
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

        await GetAndAddImageCoverAsync(seriesItem, posterTall, ImageType.Primary, cancellationToken);
        await GetAndAddImageBackdropAsync(seriesItem, posterWide, ImageType.Backdrop, cancellationToken);

        SetSeriesItemName(seriesItem, titleMetadata);
        SetSeriesItemOverview(seriesItem, titleMetadata);
        SetSeriesItemStudios(seriesItem, titleMetadata);
        SetSeriesItemCommunityRating(seriesItem, titleMetadata);

        await _libraryManager
            .UpdateItemAsync(seriesItem, seriesItem.DisplayParent, ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private async Task GetAndAddImageCoverAsync(BaseItem series, ImageSource posterTall, ImageType imageType, 
        CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureSeriesCoverImageEnabled)
        {
            return;
        }
        
        _ = await GetAndAddImage(series, posterTall, ImageType.Primary, cancellationToken);
    }

    private async Task GetAndAddImageBackdropAsync(BaseItem series, ImageSource posterWide, ImageType imageType, 
        CancellationToken cancellationToken)
    {
        if (!_config.IsFeatureSeriesBackgroundImageEnabled)
        {
            return;
        }
        
        _ = await GetAndAddImage(series, posterWide, ImageType.Backdrop, cancellationToken);
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

    private void SetSeriesItemName(BaseItem item, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesTitleEnabled)
        {
            return;
        }
        
        item.Name = titleMetadata.Title;
    }

    private void SetSeriesItemOverview(BaseItem item, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesDescriptionEnabled)
        {
            return;
        }
        
        item.Overview = titleMetadata.Description;
    }

    private void SetSeriesItemStudios(BaseItem item, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesStudioEnabled)
        {
            return;
        }
        
        item.SetStudios([titleMetadata.Studio]);
    }

    private void SetSeriesItemCommunityRating(BaseItem item, Domain.Entities.TitleMetadata titleMetadata)
    {
        if (!_config.IsFeatureSeriesRatingsEnabled)
        {
            return;
        }
        
        item.CommunityRating = titleMetadata.Rating;
    }
}