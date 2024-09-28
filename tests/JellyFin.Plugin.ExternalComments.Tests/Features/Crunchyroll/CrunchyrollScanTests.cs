using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll;

public class CrunchyrollScanTests
{
    private readonly CrunchyrollScan _sut;
    private readonly ILibraryManager _libraryManager;
    private readonly IPostScanTask[] _postScanTasks;

    public CrunchyrollScanTests()
    {
        var logger = Substitute.For<ILogger<CrunchyrollScan>>();
        _libraryManager = MockHelper.LibraryManager;
        _postScanTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => 
                Substitute.For<IPostScanTask>())
            .ToArray();
        
        _sut = new CrunchyrollScan(logger, _libraryManager, _postScanTasks);
    }

    [Fact]
    public async Task CallsPostTasks_WhenCrunchyrollTaskIsCalled_GivenArrayOfPostTasks()
    {
        //Arrange
        var items = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => SeriesFaker.Generate())
            .ToList<BaseItem>();

        _libraryManager
            .GetItemList(Arg.Any<InternalItemsQuery>())
            .Returns(items);

        //Act
        await _sut.Run(new Progress<double>(), CancellationToken.None);

        //Assert
        foreach (var postScanTask in _postScanTasks)
        {
            foreach (var item in items)
            {
                await postScanTask
                    .Received(1)
                    .RunAsync(item, Arg.Any<CancellationToken>());
            }
        }
    }
}