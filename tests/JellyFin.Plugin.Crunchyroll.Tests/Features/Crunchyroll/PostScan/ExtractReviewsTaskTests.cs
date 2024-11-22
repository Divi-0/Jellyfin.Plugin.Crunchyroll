using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class ExtractReviewsTaskTests
{
    private readonly ExtractReviewsTask _sut;
    
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;

    public ExtractReviewsTaskTests()
    {
        _mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<ExtractReviewsTask>>();
        _config = new PluginConfiguration();
        
        _config.IsWaybackMachineEnabled = true;
        
        _sut = new ExtractReviewsTask(_mediator, logger, _config);
    }
    
    public static IEnumerable<object[]> RandomSeriesAndMovieWithIds()
    {
        yield return [SeriesFaker.GenerateWithTitleId()];
        yield return [MovieFaker.GenerateWithCrunchyrollIds()];
    }

    public static IEnumerable<object[]> RandomSeriesAndMovieWithoutIds()
    {
        yield return [SeriesFaker.Generate()];
        yield return [MovieFaker.Generate()];
    }
    
    [Theory]
    [MemberData(nameof(RandomSeriesAndMovieWithIds))]
    public async Task CallsMediatorCommand_WhenSuccessful_GivenSeriesWithTitleId(BaseItem baseItem)
    {
        //Arrange
        var titleId = baseItem.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        var slugTitle = baseItem.ProviderIds[CrunchyrollExternalKeys.SeriesSlugTitle];
        
        _mediator
            .Send(new ExtractReviewsCommand { TitleId = titleId, SlugTitle = slugTitle }, 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .Received(1)
            .Send(new ExtractReviewsCommand { TitleId = titleId, SlugTitle = slugTitle },
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomSeriesAndMovieWithoutIds))]
    public async Task DoesNotCallMediatorCommand_WhenSeriesHasNoTitleId_GivenSeriesWithNoTitleId(BaseItem baseItem)
    {
        //Arrange

        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomSeriesAndMovieWithoutIds))]
    public async Task DoesNotCallMediatorCommand_WhenSeriesHasNoSlugTitle_GivenSeriesWithNoSlugTitle(BaseItem baseItem)
    {
        //Arrange
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomSeriesAndMovieWithIds))]
    public async Task DoesNotCallMediatorCommand_WhenWaybackMachineIsDisabled_GivenSeriesWithTitleId(BaseItem baseItem)
    {
        //Arrange
        
        _config.IsWaybackMachineEnabled = false;
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }
}