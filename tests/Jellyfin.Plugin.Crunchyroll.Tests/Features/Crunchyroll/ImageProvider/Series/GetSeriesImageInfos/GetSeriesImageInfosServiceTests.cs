using System.Text.Json;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ImageProvider.Series.GetSeriesImageInfos;

public class GetSeriesImageInfosServiceTests
{
    private readonly GetSeriesImageInfosService _sut;
    private readonly IGetSeriesImageInfosRepository _repository;

    public GetSeriesImageInfosServiceTests()
    {
        _repository = Substitute.For<IGetSeriesImageInfosRepository>();
        var logger = Substitute.For<ILogger<GetSeriesImageInfosService>>();
        _sut = new GetSeriesImageInfosService(_repository, logger);
    }

    [Fact]
    public async Task ReturnsImageInfos_WhenSuccessful_GivenSeriesWithSeriesId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var seriesId = series.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(series);
        var posterTall = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterTall)!;
        var posterWide = JsonSerializer.Deserialize<ImageSource>(titleMetadata.PosterWide)!;
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(series, CancellationToken.None);

        //Assert
        imageInfos.Should().HaveCount(2);
        imageInfos.Should().ContainEquivalentOf(new RemoteImageInfo
        {
            Url = posterTall.Uri,
            Width = posterTall.Width,
            Height = posterTall.Height,
            Type = ImageType.Primary
        });
        imageInfos.Should().ContainEquivalentOf(new RemoteImageInfo
        {
            Url = posterWide.Uri,
            Width = posterWide.Width,
            Height = posterWide.Height,
            Type = ImageType.Backdrop
        });

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenRepositoryGetTitleMetadataReturnedNull_GivenSeriesWithSeriesId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var seriesId = series.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(series, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenRepositoryGetTitleMetadataFails_GivenSeriesWithSeriesId()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var seriesId = series.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        //Act
        var imageInfos = await _sut.GetImageInfosAsync(series, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsEmptyImageInfos_WhenSeriesHasNoSeriesId_GivenSeriesWithoutSeriesId()
    {
        //Arrange
        var series = SeriesFaker.Generate();
        
        //Act
        var imageInfos = await _sut.GetImageInfosAsync(series, CancellationToken.None);

        //Assert
        imageInfos.Should().BeEmpty();

        await _repository
            .DidNotReceive()
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>());
    }
}