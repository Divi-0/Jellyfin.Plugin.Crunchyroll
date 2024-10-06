using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

public class SetSeriesImagesTask : IPostTitleIdSetTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IGetTitleMetadata _getTitleMetadata;
    private readonly IFile _file;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    private readonly ILogger<SetSeriesImagesTask> _logger;
    private readonly IDirectory _directory;

    public SetSeriesImagesTask(ILibraryManager libraryManager, IGetTitleMetadata getTitleMetadata,
        IFile file, ICrunchyrollSeriesClient crunchyrollSeriesClient, ILogger<SetSeriesImagesTask> logger,
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

        var posterTallResult = await GetAndAddImage(seriesItem, titleMetadata.PosterTallUri, ImageType.Primary, cancellationToken);
        var posterWideResult = await GetAndAddImage(seriesItem, titleMetadata.PosterWideUri, ImageType.Backdrop, cancellationToken);

        if (posterTallResult.IsSuccess || posterWideResult.IsSuccess)
        {
            await _libraryManager.UpdateImagesAsync(seriesItem, forceUpdate: true);
        }
    }

    private async Task<Result> GetAndAddImage(BaseItem series, string uri, ImageType imageType, CancellationToken cancellationToken)
    {
        var posterImageResult = await _crunchyrollSeriesClient.GetPosterImagesAsync(uri, cancellationToken);

        if (posterImageResult.IsFailed)
        {
            return posterImageResult.ToResult();
        }

        var directory = Path.Combine(
            Path.GetDirectoryName(typeof(SetSeriesImagesTask).Assembly.Location)!,
            "series-images");
        
        var filePath = Path.Combine(directory, Path.GetFileName(uri));
        
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
            Type = imageType
        }, 0);
        
        return Result.Ok();
    }
}