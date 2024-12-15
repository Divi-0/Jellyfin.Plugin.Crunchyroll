using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetSpecialEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;

public class EpisodeGetMetadataServiceTests
{
    private readonly EpisodeGetMetadataService _sut;
    private readonly IGetEpisodeCrunchyrollIdService _getEpisodeCrunchyrollIdService;
    private readonly IScrapEpisodeMetadataService _scrapEpisodeMetadataService;
    private readonly ISetMetadataToEpisodeService _setMetadataToEpisodeService;
    private readonly IGetSpecialEpisodeCrunchyrollIdService _specialEpisodeCrunchyrollIdService;

    public EpisodeGetMetadataServiceTests()
    {
        var logger = Substitute.For<ILogger<EpisodeGetMetadataService>>();
        _getEpisodeCrunchyrollIdService = Substitute.For<IGetEpisodeCrunchyrollIdService>();
        _scrapEpisodeMetadataService = Substitute.For<IScrapEpisodeMetadataService>();
        _setMetadataToEpisodeService = Substitute.For<ISetMetadataToEpisodeService>();
        _specialEpisodeCrunchyrollIdService = Substitute.For<IGetSpecialEpisodeCrunchyrollIdService>();
        _sut = new EpisodeGetMetadataService(logger, _getEpisodeCrunchyrollIdService, _scrapEpisodeMetadataService,
            _setMetadataToEpisodeService, _specialEpisodeCrunchyrollIdService);
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenNoSeasonIdFound_GivenEmptyParentProviderIds()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.SeasonProviderIds = new Dictionary<string, string>();
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Episode());
    }

    [Fact]
    public async Task ReturnsMetadata_WhenSuccessful_GivenValidEpisodeInfo()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var episode = EpisodeFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seriesId = new CrunchyrollId(episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]);
        var seasonId = new CrunchyrollId(episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId]);
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(episodeId);

        _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episode);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(episode);
        metadataResult.Item.ProviderIds
            .Should().Contain(new KeyValuePair<string, string>(CrunchyrollExternalKeys.EpisodeId, episodeId));
        
        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .Received(1)
            .GetEpisodeIdAsync(seasonId, seriesId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Path.GetFileNameWithoutExtension(episodeInfo.Path), episodeInfo.IndexNumber,
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .Received(1)
            .SetMetadataToEpisodeAsync(episodeId, episodeInfo.IndexNumber, episodeInfo.ParentIndexNumber,
                episodeInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsGetEpisodeId_WhenEpisodeHasAlreadyAnId_GivenEpisodeInfoWithProviderIdForEpisodeId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var episode = EpisodeFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seasonId = new CrunchyrollId(episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId]);
        
        episodeInfo.ProviderIds.Add(CrunchyrollExternalKeys.EpisodeId, episodeId);
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(episodeId);

        _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episode);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(episode);
        
        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .DidNotReceive()
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .Received(1)
            .SetMetadataToEpisodeAsync(episodeId, episodeInfo.IndexNumber, episodeInfo.ParentIndexNumber,
                episodeInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
        [Fact]
    public async Task ReturnsMetadata_WhenScrapFails_GivenValidEpisodeInfo()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var episode = EpisodeFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seriesId = new CrunchyrollId(episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]);
        var seasonId = new CrunchyrollId(episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId]);
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));

        _getEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(episodeId);

        _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episode);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(episode);
        
        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .Received(1)
            .GetEpisodeIdAsync(seasonId, seriesId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Path.GetFileNameWithoutExtension(episodeInfo.Path), episodeInfo.IndexNumber,
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .Received(1)
            .SetMetadataToEpisodeAsync(episodeId, episodeInfo.IndexNumber, episodeInfo.ParentIndexNumber,
                episodeInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenGetEpisodeCrunchyrollIdFails_GivenValidEpisodeInfo()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var seriesId = new CrunchyrollId(episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]);
        var seasonId = new CrunchyrollId(episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId]);
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _getEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Episode());
        
        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .Received(1)
            .GetEpisodeIdAsync(seasonId, seriesId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Path.GetFileNameWithoutExtension(episodeInfo.Path), episodeInfo.IndexNumber,
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .DidNotReceive()
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenGetEpisodeCrunchyrollIdReturnsNull_GivenValidEpisodeInfo()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var seriesId = new CrunchyrollId(episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]);
        var seasonId = new CrunchyrollId(episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId]);
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _getEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok<CrunchyrollId?>(null));
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Episode());
        
        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .Received(1)
            .GetEpisodeIdAsync(seasonId, seriesId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Path.GetFileNameWithoutExtension(episodeInfo.Path), episodeInfo.IndexNumber,
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .DidNotReceive()
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsHasMetadataFalse_WhenSetMetadataToEpisodeFails_GivenValidEpisodeInfo()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seriesId = new CrunchyrollId(episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]);
        var seasonId = new CrunchyrollId(episodeInfo.SeasonProviderIds[CrunchyrollExternalKeys.SeasonId]);
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _getEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            .Returns(episodeId);

        _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Episode());
        
        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .Received(1)
            .GetEpisodeIdAsync(seasonId, seriesId, episodeInfo.GetPreferredMetadataCultureInfo(),
                Path.GetFileNameWithoutExtension(episodeInfo.Path), episodeInfo.IndexNumber,
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .Received(1)
            .SetMetadataToEpisodeAsync(episodeId, episodeInfo.IndexNumber, episodeInfo.ParentIndexNumber,
                episodeInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsMetadata_WhenParentIndexIsZero_GivenEpisodeFromSpecialsSeason()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        var episode = EpisodeFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var seriesId = new CrunchyrollId(episodeInfo.SeriesProviderIds[CrunchyrollExternalKeys.SeriesId]);
        episodeInfo.ParentIndexNumber = 0;
        episodeInfo.SeasonProviderIds = new Dictionary<string, string>();

        _specialEpisodeCrunchyrollIdService
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(episodeId);

        _setMetadataToEpisodeService
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episode);
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeTrue();
        metadataResult.Item.Should().BeEquivalentTo(episode);
        metadataResult.Item.ProviderIds
            .Should().Contain(new KeyValuePair<string, string>(CrunchyrollExternalKeys.EpisodeId, episodeId));
        
        await _scrapEpisodeMetadataService
            .DidNotReceive()
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .DidNotReceive()
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>());

        await _specialEpisodeCrunchyrollIdService
            .Received(1)
            .GetEpisodeIdAsync(seriesId, Path.GetFileNameWithoutExtension(episodeInfo.Path), 
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .Received(1)
            .SetMetadataToEpisodeAsync(episodeId, episodeInfo.IndexNumber, episodeInfo.ParentIndexNumber,
                episodeInfo.GetPreferredMetadataCultureInfo(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsMetadata_WhenParentIndexIsZero_GivenEpisodeFromSpecialsSeasonWithoutSeriesId()
    {
        //Arrange
        var episodeInfo = EpisodeInfoFaker.Generate();
        episodeInfo.ParentIndexNumber = 0;
        episodeInfo.SeasonProviderIds = new Dictionary<string, string>();
        episodeInfo.SeriesProviderIds = new Dictionary<string, string>();
        
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var metadataResult = await _sut.GetMetadataAsync(episodeInfo, CancellationToken.None);

        //Assert
        metadataResult.HasMetadata.Should().BeFalse();
        metadataResult.Item.Should().BeEquivalentTo(new MediaBrowser.Controller.Entities.TV.Episode());
        
        await _scrapEpisodeMetadataService
            .DidNotReceive()
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _getEpisodeCrunchyrollIdService
            .DidNotReceive()
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<string>(), Arg.Any<int?>(),
                Arg.Any<CancellationToken>());

        await _specialEpisodeCrunchyrollIdService
            .DidNotReceive()
            .GetEpisodeIdAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), 
                Arg.Any<CancellationToken>());

        await _setMetadataToEpisodeService
            .DidNotReceive()
            .SetMetadataToEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int?>(), Arg.Any<int?>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
}