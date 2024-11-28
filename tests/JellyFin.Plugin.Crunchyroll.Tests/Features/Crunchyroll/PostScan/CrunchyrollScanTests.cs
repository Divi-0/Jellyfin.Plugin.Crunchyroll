using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class CrunchyrollScanTests
{
    private readonly CrunchyrollScan _sut;
    private readonly ILibraryManager _libraryManager;
    private readonly IPostSeriesScanTask[] _postSeriesScanTasks;
    private readonly IPostMovieScanTask[] _postMovieScanTasks;
    private readonly PluginConfiguration _config;

    public CrunchyrollScanTests()
    {
        var logger = Substitute.For<ILogger<CrunchyrollScan>>();
        _libraryManager = MockHelper.LibraryManager;
        _postSeriesScanTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => 
                Substitute.For<IPostSeriesScanTask>())
            .ToArray();
        _postMovieScanTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => 
                Substitute.For<IPostMovieScanTask>())
            .ToArray();

        _config = new PluginConfiguration();
        _config.LibraryPath = "/mnt/Crunchyroll";
        
        _sut = new CrunchyrollScan(logger, _libraryManager, _postSeriesScanTasks, _postMovieScanTasks, _config);
    }

    [Fact]
    public async Task CallsPostTasks_WhenCrunchyrollTaskIsCalled_GivenArrayOfPostTasks()
    {
        //Arrange
        var items = new List<BaseItem>();
        
        var series = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => SeriesFaker.Generate())
            .ToList<BaseItem>();
        
        var movies = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => MovieFaker.Generate())
            .ToList<BaseItem>();
        
        items.AddRange(series);
        items.AddRange(movies);

        var topParentId = Guid.NewGuid();
        _libraryManager
            .GetItemIds(Arg.Is<InternalItemsQuery>(x => x.Path == _config.LibraryPath))
            .Returns([topParentId]);

        _libraryManager
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.TopParentIds.Contains(topParentId)))
            .Returns(items);

        //Act
        await _sut.Run(new Progress<double>(), CancellationToken.None);

        //Assert
        foreach (var postScanTask in _postSeriesScanTasks)
        {
            foreach (var item in series)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
        
        foreach (var postScanTask in _postMovieScanTasks)
        {
            foreach (var item in movies)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
    }

    [Fact]
    public async Task SearchesAllItemsInRootPath_WhenTopParentByPathNotFound_GivenNotExistingLibraryPathInConfig()
    {
        //Arrange
        var items = new List<BaseItem>();
        
        var series = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => SeriesFaker.Generate())
            .ToList();
        
        var movies = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => MovieFaker.Generate())
            .ToList();
        
        items.AddRange(series);
        items.AddRange(movies);
        
        _libraryManager
            .GetItemIds(Arg.Is<InternalItemsQuery>(x => x.Path == _config.LibraryPath))
            .Returns([]);

        _libraryManager
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.TopParentIds.Length == 0))
            .Returns(items);

        //Act
        await _sut.Run(new Progress<double>(), CancellationToken.None);

        //Assert
        foreach (var postScanTask in _postSeriesScanTasks)
        {
            foreach (var item in series)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
        
        foreach (var postScanTask in _postMovieScanTasks)
        {
            foreach (var item in movies)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
    }

    [Fact]
    public async Task SearchesAllItemsInRootPath_WhenLibraryPathIsEmpty_GivenEmptyLibraryPathInConfig()
    {
        //Arrange
        var items = new List<BaseItem>();
        
        var series = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => SeriesFaker.Generate())
            .ToList<BaseItem>();
        
        var movies = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => MovieFaker.Generate())
            .ToList<BaseItem>();
        
        items.AddRange(series);
        items.AddRange(movies);

        _config.LibraryPath = string.Empty;
        
        _libraryManager
            .GetItemList(Arg.Is<InternalItemsQuery>(x => x.TopParentIds.Length == 0))
            .Returns(items);

        //Act
        await _sut.Run(new Progress<double>(), CancellationToken.None);

        //Assert
        foreach (var postScanTask in _postSeriesScanTasks)
        {
            foreach (var item in series)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
        
        foreach (var postScanTask in _postMovieScanTasks)
        {
            foreach (var item in movies)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
    }

    [Fact]
    public async Task SkipsScan_WhenConfigIsInvalid_GivenEmptyCrunchyrollUrl()
    {
        //Arrange
        _config.CrunchyrollUrl = string.Empty;

        //Act
        await _sut.Run(new Progress<double>(), CancellationToken.None);

        //Assert
        foreach (var postScanTask in _postSeriesScanTasks)
        {
            await postScanTask
                .DidNotReceive()
                .RunAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>());
        }
        
        foreach (var postScanTask in _postMovieScanTasks)
        {
            await postScanTask
                .DidNotReceive()
                .RunAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task SkipsScan_WhenConfigIsInvalid_GivenEmptyArchiveOrgUrl()
    {
        //Arrange
        _config.IsWaybackMachineEnabled = true;
        _config.ArchiveOrgUrl = string.Empty;

        //Act
        await _sut.Run(new Progress<double>(), CancellationToken.None);

        //Assert
        foreach (var postScanTask in _postSeriesScanTasks)
        {
            await postScanTask
                .DidNotReceive()
                .RunAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>());
        }
        
        foreach (var postScanTask in _postMovieScanTasks)
        {
            await postScanTask
                .DidNotReceive()
                .RunAsync(Arg.Any<BaseItem>(), Arg.Any<CancellationToken>());
        }
    }
}