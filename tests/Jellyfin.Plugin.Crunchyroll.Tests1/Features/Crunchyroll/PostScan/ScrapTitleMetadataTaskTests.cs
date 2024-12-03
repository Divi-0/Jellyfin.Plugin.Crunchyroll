using System.Globalization;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class ScrapTitleMetadataTaskTests
{
    private readonly ScrapTitleMetadataTask _sut;

    private readonly IMediator _mediator;
    private readonly IMediaSourceManager _mediaSourceManager;
    
    public ScrapTitleMetadataTaskTests()
    {
        _mediator = Substitute.For<IMediator>();
        _mediaSourceManager = MockHelper.MediaSourceManager;
        var logger = Substitute.For<ILogger<ScrapTitleMetadataTask>>();
        
        _sut = new ScrapTitleMetadataTask(_mediator, logger);
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
        
        _mediaSourceManager
            .GetPathProtocol(baseItem.Path)
            .Returns(MediaProtocol.File);
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .Received(1)
            .Send(new ScrapTitleMetadataCommand
                {
                    TitleId = titleId, 
                    Language = new CultureInfo("en-US"),
                    MovieSeasonId = baseItem is Movie ? baseItem.ProviderIds[CrunchyrollExternalKeys.SeasonId] : null,
                    MovieEpisodeId = baseItem is Movie ? baseItem.ProviderIds[CrunchyrollExternalKeys.EpisodeId] : null
                },
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomSeriesAndMovieWithoutIds))]
    public async Task DoesNotCallMediatorCommand_WhenSeriesHasNoTitleId_GivenSeriesWithNoTitleId(BaseItem baseItem)
    {
        //Arrange
        _mediaSourceManager
            .GetPathProtocol(baseItem.Path)
            .Returns(MediaProtocol.File);
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ScrapTitleMetadataCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomSeriesAndMovieWithoutIds))]
    public async Task DoesNotCallMediatorCommand_WhenSeriesHasNoSlugTitle_GivenSeriesWithNoSlugTitle(BaseItem baseItem)
    {
        //Arrange
        _mediaSourceManager
            .GetPathProtocol(baseItem.Path)
            .Returns(MediaProtocol.File);
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ScrapTitleMetadataCommand>(), Arg.Any<CancellationToken>());
    }
}