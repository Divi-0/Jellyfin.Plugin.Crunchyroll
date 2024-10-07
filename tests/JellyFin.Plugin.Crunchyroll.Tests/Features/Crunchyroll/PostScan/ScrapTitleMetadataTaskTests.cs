using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Mediator;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class ScrapTitleMetadataTaskTests
{
    private readonly ScrapTitleMetadataTask _sut;

    private readonly IMediator _mediator;
    
    public ScrapTitleMetadataTaskTests()
    {
        _mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<ScrapTitleMetadataTask>>();
        
        _sut = new ScrapTitleMetadataTask(_mediator, logger);
    }

    [Fact]
    public async Task CallsMediatorCommand_WhenSuccessful_GivenSeriesWithTitleId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var titleId = series.ProviderIds[CrunchyrollExternalKeys.Id];
        var slugTitle = series.ProviderIds[CrunchyrollExternalKeys.SlugTitle];
        
        _mediator
            .Send(new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle }, 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .Received(1)
            .Send(new ScrapTitleMetadataCommand { TitleId = titleId, SlugTitle = slugTitle },
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
            .Send(Arg.Any<ScrapTitleMetadataCommand>(), Arg.Any<CancellationToken>());
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
            .Send(Arg.Any<ScrapTitleMetadataCommand>(), Arg.Any<CancellationToken>());
    }
}