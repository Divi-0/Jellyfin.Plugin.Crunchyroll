using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.SetMetadataToSeason;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Season;

public class SetMetadataToSeasonServiceTests
{
    private readonly SetMetadataToSeasonService _sut;
    private readonly ISetMetadataToSeasonRepository _repository;
    private readonly PluginConfiguration _config;

    public SetMetadataToSeasonServiceTests()
    {
        _repository = Substitute.For<ISetMetadataToSeasonRepository>();
        var logger = Substitute.For<ILogger<SetMetadataToSeasonService>>();
        _config = new PluginConfiguration
        {
            IsFeatureSeasonTitleEnabled = true,
            IsFeatureSeasonOrderByCrunchyrollOrderEnabled = true
        };
        _sut = new SetMetadataToSeasonService(_repository, logger, _config);
    }
    
    [Fact]
    public async Task SetsMetadata_WhenSuccessful_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        var indexNumber = Random.Shared.Next(1, int.MaxValue);

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var seasonWithMetadata = setMetadataResult.Value;
        seasonWithMetadata.Name.Should().Be($"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}");
        seasonWithMetadata.IndexNumber.Should().Be(indexNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task NameDoesNotContainSeasonNumber_WhenSeasonDisplayNumberIsEmpty_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        crunchyrollSeason = crunchyrollSeason with { SeasonDisplayNumber = string.Empty };
        var indexNumber = Random.Shared.Next(1, int.MaxValue);
        

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var seasonWithMetadata = setMetadataResult.Value;
        seasonWithMetadata.Name.Should().Be(crunchyrollSeason.Title);
        seasonWithMetadata.IndexNumber.Should().Be(indexNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenSeasonIdIsNull_GivenSeasonWithoutSeasonId()
    {
        //Arrange
        var seasonId = (CrunchyrollId?)null;
        var language = new CultureInfo("en-US");
        var indexNumber = Random.Shared.Next(1, int.MaxValue);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId!, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(ErrorCodes.NotAllowed);
        
        await _repository
            .DidNotReceive()
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetSeasonFails_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var indexNumber = Random.Shared.Next(1, int.MaxValue);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(error);
        
        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task OrdersSeasonsBySeasonSequenceNumber_WhenFeatureOrderByCrunchyrollOrderIsEnabled_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        var indexNumber = Random.Shared.Next(1, int.MaxValue);
        
        _config.IsFeatureSeasonOrderByCrunchyrollOrderEnabled = true;

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var seasonWithMetadata = setMetadataResult.Value;
        seasonWithMetadata.Name.Should().Be($"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}");
        seasonWithMetadata.ForcedSortName.Should().Be(crunchyrollSeason.SeasonSequenceNumber.ToString());
        seasonWithMetadata.PresentationUniqueKey.Should().NotBeEmpty();
        seasonWithMetadata.IndexNumber.Should().Be(indexNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotOrdersSeasonsBySeasonSequenceNumber_WhenFeatureOrderByCrunchyrollOrderIsDisabled_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        var indexNumber = Random.Shared.Next(1, int.MaxValue);
        
        _config.IsFeatureSeasonOrderByCrunchyrollOrderEnabled = false;

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var seasonWithMetadata = setMetadataResult.Value;
        seasonWithMetadata.Name.Should().Be($"S{crunchyrollSeason.SeasonDisplayNumber}: {crunchyrollSeason.Title}");
        seasonWithMetadata.IndexNumber.Should().NotBe(crunchyrollSeason.SeasonSequenceNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetSeasonReturnsNull_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var indexNumber = Random.Shared.Next(1, int.MaxValue);

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Season?>(null));

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(ErrorCodes.NotFound);
        
        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotSetTitle_WhenFeatureSeasonTitleIsDisabled_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        var indexNumber = Random.Shared.Next(1, int.MaxValue);

        _config.IsFeatureSeasonTitleEnabled = false;

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var seasonWithMetadata = setMetadataResult.Value;
        seasonWithMetadata.Name.Should().NotContain(crunchyrollSeason.Title);
        seasonWithMetadata.IndexNumber.Should().Be(indexNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsIndexNumber_WhenCurrentIndexNumberIsNull_GivenSeasonWithSeasonId()
    {
        //Arrange
        var seasonId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        var indexNumber = (int?)null;

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeason);

        //Act
        var setMetadataResult = await _sut.SetMetadataToSeasonAsync(seasonId, language, indexNumber, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var seasonWithMetadata = setMetadataResult.Value;
        seasonWithMetadata.IndexNumber.Should().Be(int.MaxValue - crunchyrollSeason.SeasonSequenceNumber);

        await _repository
            .Received(1)
            .GetSeasonAsync(seasonId,
                language, Arg.Any<CancellationToken>());
    }
}