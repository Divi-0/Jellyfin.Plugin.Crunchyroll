using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
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
    private readonly IGetTitleMetadata _getTitleMetadata;
    private readonly IFile _file;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly ILogger<OverwriteSeriesJellyfinDataTask> _logger;
    private readonly IDirectory _directory;

    public OverwriteSeriesJellyfinDataTask(ILibraryManager libraryManager, IGetTitleMetadata getTitleMetadata,
        IFile file, ICrunchyrollSeriesClient crunchyrollSeriesClient, ILogger<OverwriteSeriesJellyfinDataTask> logger,
        IDirectory directory)
    {
        _libraryManager = libraryManager;
        _getTitleMetadata = getTitleMetadata;
        _file = file;
        _crunchyrollSeriesClient = crunchyrollSeriesClient;
        _logger = logger;
        _directory = directory;
    }
    
    public async Task RunAsync(BaseItem seriesItem, CancellationToken cancellationToken)
    {
        var hasTitleId = seriesItem.ProviderIds.TryGetValue(CrunchyrollExternalKeys.Id, out var titleId) &&
                    !string.IsNullOrWhiteSpace(titleId);
        
        if (!hasTitleId)
        {
            return;
        }
        
        var titleMetadata = await _getTitleMetadata.GetTitleMetadataAsync(titleId!);

        if (titleMetadata is null)
        {
            _logger.LogDebug("No TitleMetadata for series with titleId {TitleId} found. Skipping...", titleId);
            return;
        }

        _ = await GetAndAddImage(seriesItem, titleMetadata.PosterTall, ImageType.Primary, cancellationToken);
        _ = await GetAndAddImage(seriesItem, titleMetadata.PosterWide, ImageType.Backdrop, cancellationToken);
        
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
        
        if (currentImage is not null &&
            currentImage.Path == filePath && 
            currentImage.Type == imageType &&
            currentImage.Width == imageSource.Width &&
            currentImage.Height == imageSource.Height)
        {
            _logger.LogDebug("Image with type {Type} for item with Name {Name} already exists, skipping...", 
                imageType,
                series.Name);
            return Result.Ok();
        }
        
        var posterImageResult = await _crunchyrollSeriesClient.GetPosterImagesAsync(imageSource.Uri, cancellationToken);

        if (posterImageResult.IsFailed)
        {
            return posterImageResult.ToResult();
        }
        
        try
        {
            if (!_directory.Exists(directory))
            {
                _directory.CreateDirectory(directory);
            }
            
            await using var fileStream = _file.Create(filePath);
            await posterImageResult.Value.CopyToAsync(fileStream, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "unknown error, while creating poster image on filesystem");
            return Result.Fail("FileSystem error");
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
}