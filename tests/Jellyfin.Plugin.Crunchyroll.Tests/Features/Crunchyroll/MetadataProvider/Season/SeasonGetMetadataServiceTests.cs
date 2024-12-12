using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Season;

public class SeasonGetMetadataServiceTests
{
    private readonly SeasonGetMetadataService _sut;
    private readonly IGetSeasonCrunchyrollIdService _getSeasonCrunchyrollIdService;
    private readonly IScrapSeasonMetadataService _scrapSeasonMetadataService;
    private readonly ISetMetadataToSeasonService _setMetadataToSeasonService;

    public SeasonGetMetadataServiceTests()
    {
        var logger = Substitute.For<ILogger<SeasonGetMetadataService>>();
        _getSeasonCrunchyrollIdService = Substitute.For<IGetSeasonCrunchyrollIdService>();
        _scrapSeasonMetadataService = Substitute.For<IScrapSeasonMetadataService>();
        _setMetadataToSeasonService = Substitute.For<ISetMetadataToSeasonService>();
        _sut = new SeasonGetMetadataService(logger, _getSeasonCrunchyrollIdService, _scrapSeasonMetadataService,
            _setMetadataToSeasonService);
    }

    [Fact]
    public async Task ReturnsHasNoMetadata_WhenHasNoSeriesId_GivenSeasonInfoWithoutSeriesIdParent()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        seasonInfo.SeriesProviderIds = new Dictionary<string, string>();
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Season());
    }

    [Fact]
    public async Task ReturnsHasMetadata_WhenSuccessful_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var seasonId = CrunchyrollIdFaker.Generate();
        var expectedLanguage = new CultureInfo($"{seasonInfo.MetadataLanguage}-{seasonInfo.MetadataCountryCode}");

        var newSeason = SeasonFaker.Generate();
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getSeasonCrunchyrollIdService
            .GetSeasonCrunchyrollId(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonId);

        _setMetadataToSeasonService
            .SetMetadataToSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(newSeason);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        
        metadataResult.Item.Should().BeEquivalentTo(newSeason);
        metadataResult.Item.ProviderIds
            .Should().Contain(new KeyValuePair<string, string>(CrunchyrollExternalKeys.SeasonId, seasonId));
        
        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, expectedLanguage, 
                Arg.Any<CancellationToken>());

        await _getSeasonCrunchyrollIdService
            .Received(1)
            .GetSeasonCrunchyrollId(seriesId, Path.GetFileNameWithoutExtension(seasonInfo.Path), seasonInfo.IndexNumber,
                expectedLanguage, Arg.Any<CancellationToken>());

        await _setMetadataToSeasonService
            .Received(1)
            .SetMetadataToSeasonAsync(seasonId, expectedLanguage, seasonInfo.IndexNumber,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsHasMetadata_WhenSrapFailsButGetSeasonIdSucceeds_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var seasonId = CrunchyrollIdFaker.Generate();
        var expectedLanguage = new CultureInfo($"{seasonInfo.MetadataLanguage}-{seasonInfo.MetadataCountryCode}");

        var newSeason = SeasonFaker.Generate();
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        _getSeasonCrunchyrollIdService
            .GetSeasonCrunchyrollId(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonId);

        _setMetadataToSeasonService
            .SetMetadataToSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(newSeason);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        
        newSeason.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonId;
        metadataResult.Item.Should().BeEquivalentTo(newSeason);
        
        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, expectedLanguage, 
                Arg.Any<CancellationToken>());

        await _getSeasonCrunchyrollIdService
            .Received(1)
            .GetSeasonCrunchyrollId(seriesId, Path.GetFileNameWithoutExtension(seasonInfo.Path), seasonInfo.IndexNumber,
                expectedLanguage, Arg.Any<CancellationToken>());

        await _setMetadataToSeasonService
            .Received(1)
            .SetMetadataToSeasonAsync(seasonId, expectedLanguage, seasonInfo.IndexNumber,
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasNoMetadata_WhenGetCrunchyrollIdFails_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var seasonId = CrunchyrollIdFaker.Generate();
        var expectedLanguage = new CultureInfo($"{seasonInfo.MetadataLanguage}-{seasonInfo.MetadataCountryCode}");
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _getSeasonCrunchyrollIdService
            .GetSeasonCrunchyrollId(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Season());
        
        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, expectedLanguage, 
                Arg.Any<CancellationToken>());

        await _getSeasonCrunchyrollIdService
            .Received(1)
            .GetSeasonCrunchyrollId(seriesId, Path.GetFileNameWithoutExtension(seasonInfo.Path), seasonInfo.IndexNumber,
                expectedLanguage, Arg.Any<CancellationToken>());

        await _setMetadataToSeasonService
            .DidNotReceive()
            .SetMetadataToSeasonAsync(seasonId, expectedLanguage, seasonInfo.IndexNumber,
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasNoMetadata_WhenGetCrunchyrollIdReturnsNull_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]!;
        var seasonId = CrunchyrollIdFaker.Generate();
        var expectedLanguage = new CultureInfo($"{seasonInfo.MetadataLanguage}-{seasonInfo.MetadataCountryCode}");
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _getSeasonCrunchyrollIdService
            .GetSeasonCrunchyrollId(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<CrunchyrollId?>(new CrunchyrollId(null!)));
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Season());
        
        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, expectedLanguage, 
                Arg.Any<CancellationToken>());

        await _getSeasonCrunchyrollIdService
            .Received(1)
            .GetSeasonCrunchyrollId(seriesId, Path.GetFileNameWithoutExtension(seasonInfo.Path), seasonInfo.IndexNumber,
                expectedLanguage, Arg.Any<CancellationToken>());

        await _setMetadataToSeasonService
            .DidNotReceive()
            .SetMetadataToSeasonAsync(seasonId, expectedLanguage, seasonInfo.IndexNumber,
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasNoMetadata_WhenSetMetadataFails_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var seasonId = CrunchyrollIdFaker.Generate();
        var expectedLanguage = new CultureInfo($"{seasonInfo.MetadataLanguage}-{seasonInfo.MetadataCountryCode}");
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getSeasonCrunchyrollIdService
            .GetSeasonCrunchyrollId(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonId);

        _setMetadataToSeasonService
            .SetMetadataToSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Season());
        
        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, expectedLanguage, 
                Arg.Any<CancellationToken>());

        await _getSeasonCrunchyrollIdService
            .Received(1)
            .GetSeasonCrunchyrollId(seriesId, Path.GetFileNameWithoutExtension(seasonInfo.Path), seasonInfo.IndexNumber,
                expectedLanguage, Arg.Any<CancellationToken>());

        await _setMetadataToSeasonService
            .Received(1)
            .SetMetadataToSeasonAsync(seasonId, expectedLanguage, seasonInfo.IndexNumber,
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataAndDoesNotCallGetSeasonCrunchyrollId_WhenSeasonHasAlreadyAnId_GivenSeasonInfo()
    {
        //Arrange
        var seasonInfo = SeasonInfoFaker.Generate();
        var seriesId = seasonInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId];
        var seasonId = CrunchyrollIdFaker.Generate();
        seasonInfo.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonId;
        var expectedLanguage = new CultureInfo($"{seasonInfo.MetadataLanguage}-{seasonInfo.MetadataCountryCode}");

        var newSeason = SeasonFaker.Generate();
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _setMetadataToSeasonService
            .SetMetadataToSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(newSeason);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(seasonInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        
        newSeason.ProviderIds[CrunchyrollExternalKeys.SeasonId] = seasonId;
        metadataResult.Item.Should().BeEquivalentTo(newSeason);
        
        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, expectedLanguage, 
                Arg.Any<CancellationToken>());

        await _getSeasonCrunchyrollIdService
            .DidNotReceive()
            .GetSeasonCrunchyrollId(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _setMetadataToSeasonService
            .Received(1)
            .SetMetadataToSeasonAsync(seasonId, expectedLanguage, seasonInfo.IndexNumber,
                Arg.Any<CancellationToken>());
    }
}