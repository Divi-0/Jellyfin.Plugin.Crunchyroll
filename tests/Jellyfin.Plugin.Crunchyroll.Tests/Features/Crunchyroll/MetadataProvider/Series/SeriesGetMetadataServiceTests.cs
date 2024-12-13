using System.Globalization;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.GetSeriesCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.SetMetadataToSeries;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series;

public class SeriesGetMetadataServiceTests
{
    private readonly SeriesGetMetadataService _sut;
    private readonly IGetSeriesCrunchyrollIdService _getSeriesCrunchyrollIdService;
    private readonly IScrapSeriesMetadataService _scrapSeriesMetadataService;
    private readonly ISetMetadataToSeriesService _setMetadataToSeriesService;
    private readonly IMediaSourceManager _mediaSourceManager;
    
    private readonly Faker _faker;

    public SeriesGetMetadataServiceTests()
    {
        _getSeriesCrunchyrollIdService = Substitute.For<IGetSeriesCrunchyrollIdService>();
        _scrapSeriesMetadataService = Substitute.For<IScrapSeriesMetadataService>();
        _setMetadataToSeriesService = Substitute.For<ISetMetadataToSeriesService>();
        _mediaSourceManager = MockHelper.MediaSourceManager;
        var logger = Substitute.For<ILogger<SeriesGetMetadataService>>();
        
            _sut = new SeriesGetMetadataService(_getSeriesCrunchyrollIdService, _scrapSeriesMetadataService,
            _setMetadataToSeriesService, logger);

        _faker = new Faker();
    }
    
    [Fact]
    public async Task SetsTitleId_WhenTitleIdWasReturned_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var crunchrollId = CrunchyrollIdFaker.Generate();
        var series = SeriesFaker.Generate();
        var seriesInfo = SeriesInfoFaker.Generate(series);
        var newSeries = SeriesFaker.Generate();

        _getSeriesCrunchyrollIdService
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchrollId);

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _setMetadataToSeriesService
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newSeries);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        await _getSeriesCrunchyrollIdService
            .Received(1)
            .GetSeriesCrunchyrollId(series.FileNameWithoutExtension, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(newSeries, opt => opt
            .Excluding(x => x.ProviderIds));
        
        metadataResult.Item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
        actualCrunchyrollId.Should().Be(crunchrollId);
    }
    
    [Fact]
    public async Task SetsTitleId_WhenFileNameHasCrunchyrollIdAttribute_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var crunchrollId = CrunchyrollIdFaker.Generate();
        var series = SeriesFaker.Generate();
        series.Path = $"{series.Path} [CrunchyrollId-{crunchrollId}]";
        var seriesInfo = SeriesInfoFaker.Generate(series);
        var newSeries = SeriesFaker.Generate();

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);
        
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _setMetadataToSeriesService
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newSeries);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        await _getSeriesCrunchyrollIdService
            .DidNotReceive()
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(newSeries, opt => opt
            .Excluding(x => x.ProviderIds));
        
        metadataResult.Item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
        actualCrunchyrollId.Should().Be(crunchrollId);
    }
    
    [Fact]
    public async Task OverwriteExistingTitleId_WhenFileNameHasCrunchyrollIdAttribute_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var crunchrollId = CrunchyrollIdFaker.Generate();
        var series = SeriesFaker.GenerateWithTitleId();
        series.Path = $"{series.Path} [CrunchyrollId-{crunchrollId}]";
        var seriesInfo = SeriesInfoFaker.Generate(series);
        var newSeries = SeriesFaker.Generate();
        
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _setMetadataToSeriesService
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newSeries);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        await _getSeriesCrunchyrollIdService
            .DidNotReceive()
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(newSeries, opt => opt
            .Excluding(x => x.ProviderIds));
        
        metadataResult.Item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
        actualCrunchyrollId.Should().Be(crunchrollId);
    }
    
    [Fact]
    public async Task CallsTitleIdQueryOnlyWithSeriesName_WhenFileNameHasYearAndDbId_GivenFileNameWithExtraLetters()
    {
        //Arrange
        var crunchrollId = CrunchyrollIdFaker.Generate();
        var series = SeriesFaker.Generate();
        var seriesName = _faker.Random.Words(3);
        series.Path = $"{_faker.Random.Word()}/{seriesName} ({_faker.Random.Number()}) [tmdbid-{_faker.Random.Number()}]";
        var seriesInfo = SeriesInfoFaker.Generate(series);
        var newSeries = SeriesFaker.Generate();

        _getSeriesCrunchyrollIdService
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(crunchrollId));

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _setMetadataToSeriesService
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(newSeries);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        await _getSeriesCrunchyrollIdService
            .Received(1)
            .GetSeriesCrunchyrollId(seriesName, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(newSeries, opt => opt
            .Excluding(x => x.ProviderIds));
        
        metadataResult.Item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
        actualCrunchyrollId.Should().Be(crunchrollId);
    }
    
    [Fact]
    public async Task SetsEmptySeriesId_WhenNoTitleIdWasFound_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        var seriesInfo = SeriesInfoFaker.Generate(series);

        _getSeriesCrunchyrollIdService
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(null));

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        await _getSeriesCrunchyrollIdService
            .Received(1)
            .GetSeriesCrunchyrollId(series.FileNameWithoutExtension, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Series(), opt => opt
            .Excluding(x => x.ProviderIds));
        
        metadataResult.Item.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var actualCrunchyrollId).Should().BeTrue();
        actualCrunchyrollId.Should().BeEmpty();

        await _scrapSeriesMetadataService
            .DidNotReceive()
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToSeriesService
            .DidNotReceive()
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenGetSeriesIdFails_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        var seriesInfo = SeriesInfoFaker.Generate(series);

        _getSeriesCrunchyrollIdService
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Series());
        
        await _getSeriesCrunchyrollIdService
            .Received(1)
            .GetSeriesCrunchyrollId(series.FileNameWithoutExtension, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _scrapSeriesMetadataService
            .DidNotReceive()
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToSeriesService
            .DidNotReceive()
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenScrapSeriesMetadataFails_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var crunchrollId = CrunchyrollIdFaker.Generate();
        var series = SeriesFaker.Generate();
        var seriesInfo = SeriesInfoFaker.Generate(series);

        _getSeriesCrunchyrollIdService
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchrollId);
        
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Series());
        
        await _getSeriesCrunchyrollIdService
            .Received(1)
            .GetSeriesCrunchyrollId(series.FileNameWithoutExtension, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(crunchrollId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToSeriesService
            .DidNotReceive()
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenSetMetadataToSeriesFails_GivenTitleWithNoCrunchyrollTitleId()
    {
        //Arrange
        var crunchrollId = CrunchyrollIdFaker.Generate();
        var series = SeriesFaker.Generate();
        var seriesInfo = SeriesInfoFaker.Generate(series);

        _getSeriesCrunchyrollIdService
            .GetSeriesCrunchyrollId(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchrollId);
        
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _setMetadataToSeriesService
            .SetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        _mediaSourceManager
            .GetPathProtocol(series.Path)
            .Returns(MediaProtocol.File);

        //Act
        var metadataResult = await _sut.GetMetadataAsync(seriesInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Series());
        
        await _getSeriesCrunchyrollIdService
            .Received(1)
            .GetSeriesCrunchyrollId(series.FileNameWithoutExtension, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(crunchrollId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToSeriesService
            .Received(1)
            .SetSeriesMetadataAsync(crunchrollId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
}