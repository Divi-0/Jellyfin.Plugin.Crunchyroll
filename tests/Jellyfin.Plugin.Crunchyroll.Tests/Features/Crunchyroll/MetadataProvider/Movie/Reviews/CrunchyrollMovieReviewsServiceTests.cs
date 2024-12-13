using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Movie.Reviews;

public class CrunchyrollMovieReviewsServiceTests
{
    private readonly CrunchyrollMovieReviewsService _sut;
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;

    public CrunchyrollMovieReviewsServiceTests()
    {
        _mediator = Substitute.For<IMediator>();
        _config = new PluginConfiguration
        {
            IsFeatureReviewsEnabled = true
        };
        _sut = new CrunchyrollMovieReviewsService(_mediator, _config);
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenSuccessful_GivenSeriesWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var seriesId = movie.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        var language = movie.GetPreferredMetadataCultureInfo();

        _mediator
            .Send(Arg.Any<ExtractReviewsCommand>())
            .Returns(Result.Ok());
        
        //Act
        var itemUpdateType = await _sut.ScrapReviewsAsync(movie, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .Received(1)
            .Send(Arg.Is<ExtractReviewsCommand>(x =>
                x.TitleId == seriesId &&
                x.Language.Name == language.Name),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenMediatorFailed_GivenSeriesWithIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();
        var seriesId = movie.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        var language = movie.GetPreferredMetadataCultureInfo();

        _mediator
            .Send(Arg.Any<ExtractReviewsCommand>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));
        
        //Act
        var itemUpdateType = await _sut.ScrapReviewsAsync(movie, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .Received(1)
            .Send(Arg.Is<ExtractReviewsCommand>(x =>
                x.TitleId == seriesId &&
                x.Language.Name == language.Name),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenHasNoSeriesId_GivenSeriesWithoutIds()
    {
        //Arrange
        var movie = MovieFaker.Generate();
        
        //Act
        var itemUpdateType = await _sut.ScrapReviewsAsync(movie, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenFeatureReviewsIsDisabled_GivenSeriesWithoutIds()
    {
        //Arrange
        var movie = MovieFaker.GenerateWithCrunchyrollIds();

        _config.IsFeatureReviewsEnabled = false;
        
        //Act
        var itemUpdateType = await _sut.ScrapReviewsAsync(movie, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractReviewsCommand>(), Arg.Any<CancellationToken>());
    }
}