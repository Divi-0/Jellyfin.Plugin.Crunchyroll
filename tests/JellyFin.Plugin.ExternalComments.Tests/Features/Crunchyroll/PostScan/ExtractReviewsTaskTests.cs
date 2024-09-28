using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker;
using Mediator;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.PostScan;

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
    
    [Fact]
    public async Task CallsMediatorCommand_WhenSuccessful_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var titleId = series.ProviderIds[CrunchyrollExternalKeys.Id];
        var slugTitle = series.ProviderIds[CrunchyrollExternalKeys.SlugTitle];
        
        _mediator
            .Send(new ExtractReviewsCommand { TitleId = titleId, SlugTitle = slugTitle }, 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .Received(1)
            .Send(new ExtractReviewsCommand { TitleId = titleId, SlugTitle = slugTitle },
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotCallMediatorCommand_WhenSeriesHasNoTitleId_GivenSeriesWithNoTitleId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotCallMediatorCommand_WhenSeriesHasNoSlugTitle_GivenSeriesWithNoSlugTitle()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotCallMediatorCommand_WhenWaybackMachineIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        
        _config.IsWaybackMachineEnabled = false;
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }
}