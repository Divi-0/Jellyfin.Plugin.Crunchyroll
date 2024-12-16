using System.Globalization;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.GetSeasonCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Season;

public class GetSeasonCrunchyrollIdServiceTests
{
    private readonly GetSeasonCrunchyrollIdService _sut;
    private readonly IGetSeasonCrunchyrollIdRepository _repository;

    private readonly Faker _faker;

    public GetSeasonCrunchyrollIdServiceTests()
    {
        _repository = Substitute.For<IGetSeasonCrunchyrollIdRepository>();
        var logger = Substitute.For<ILogger<GetSeasonCrunchyrollIdService>>();
        
        _sut = new GetSeasonCrunchyrollIdService(_repository, logger);

        _faker = new Faker();
    }
    
    [Fact]
    public async Task ReturnsCrunchyrollId_WhenIdFoundWithName_GivenItemWithUniqueName()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        season.IndexNumber = null;
        var crunchyrollSeasonId = CrunchyrollIdFaker.Generate();
        var seasonFolderName = Path.GetFileNameWithoutExtension(season.Path);

        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeasonId);

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
            seasonFolderName, season.IndexNumber,
            season.GetPreferredMetadataCultureInfo(), CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsSuccess.Should().BeTrue();
        crunchyrollIdResult.Value.Should().Be(crunchyrollSeasonId);
        
        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], seasonFolderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsCrunchyrollId_WhenFileNameIsDefaultFormat_GivenItemWithUniqueName()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        season.Path = $"/{_faker.Random.Words(3)}/Season {season.IndexNumber}";
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        var crunchyrollSeasonId = CrunchyrollIdFaker.Generate();
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);

        _repository
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeasonId);

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsSuccess.Should().BeTrue();
        crunchyrollIdResult.Value.Should().Be(crunchyrollSeasonId);

        await _repository
            .DidNotReceive()
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetSeasonIdByNumberAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], season.IndexNumber!.Value, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Theory]
    [InlineData("(2019)")]
    [InlineData("(111111111)")]
    [InlineData("[id=1234]")]
    [InlineData("[tv=DwaDWADA342w]")]
    public async Task ReturnsCrunchyrollId_WhenFileNameFormatIsSeasonNumberWithName_GivenItemWithUniqueName(
        string attributeInFolderName)
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var seriesId = series.ProviderIds[CrunchyrollExternalKeys.SeriesId];
        var season = SeasonFaker.Generate(series);
        var title = _faker.Random.Words(Random.Shared.Next(1, 10));
        season.Path = $"/{_faker.Random.Words(3)}/Season {season.IndexNumber} - {title} {attributeInFolderName}";
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        var crunchyrollSeasonId = CrunchyrollIdFaker.Generate();
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollSeasonId);

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsSuccess.Should().BeTrue();
        crunchyrollIdResult.Value.Should().Be(crunchyrollSeasonId);

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(seriesId, title, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetSeasonIdByNameFails_GivenItemHasUniqueName()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        var folderName = Path.GetFileNameWithoutExtension(season.Path);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsFailed.Should().BeTrue();
        crunchyrollIdResult.Errors.First().Message.Should().Be(error);

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetSeasonIdByNumberFails_GivenItemHasUniqueName()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsFailed.Should().BeTrue();
        crunchyrollIdResult.Errors.First().Message.Should().Be(error);

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenItemHasNoIndexNumberAndSearchByNameWasNotFound_GivenItemWithUniqueName()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        season.IndexNumber = null;
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsFailed.Should().BeTrue();
        crunchyrollIdResult.Errors.First().Message.Should().Be(Domain.Constants.ErrorCodes.Internal);

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsNull_WhenSeasonNotFound_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetAllSeasonsAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Season[]>([]));

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsSuccess.Should().BeTrue();
        crunchyrollIdResult.Value.Should().BeNull();

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetSeasonIdByNumberAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], season.IndexNumber!.Value, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetAllSeasonsAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsId_WhenSeasonFoundByIdentifier_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.GenerateWithSeasonId(series);
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate(season);
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetAllSeasonsAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Season[]>([crunchyrollSeason, CrunchyrollSeasonFaker.Generate()]));

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, crunchyrollSeason.SeasonNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsSuccess.Should().BeTrue();
        crunchyrollIdResult.Value.Should().Be(new CrunchyrollId(crunchyrollSeason.CrunchyrollId));

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetSeasonIdByNumberAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], crunchyrollSeason.SeasonNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetAllSeasonsAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsNull_WhenSeasonNotFoundByIdentifier_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetAllSeasonsAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Season[]>([CrunchyrollSeasonFaker.Generate(), CrunchyrollSeasonFaker.Generate()]));

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsSuccess.Should().BeTrue();
        crunchyrollIdResult.Value.Should().BeNull();

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetSeasonIdByNumberAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], season.IndexNumber!.Value, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetAllSeasonsAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetAllSeasonsFails_GivenTitleIdAndSeasonNumberAndDuplicateNumberOne()
    {
        //Arrange
        var series = SeriesFaker.GenerateWithTitleId();
        var season = SeasonFaker.Generate(series);
        var folderName = Path.GetFileNameWithoutExtension(season.Path);
        
        _repository
            .GetSeasonIdByNameAsync(Arg.Any<CrunchyrollId>(), Arg.Any<string>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);
        
        _repository
            .GetSeasonIdByNumberAsync(Arg.Any<CrunchyrollId>(), Arg.Any<int>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns((CrunchyrollId?)null);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetAllSeasonsAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var crunchyrollIdResult = await _sut.GetSeasonCrunchyrollId(series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
            folderName, season.IndexNumber, season.GetPreferredMetadataCultureInfo(), 
            CancellationToken.None);

        //Assert
        crunchyrollIdResult.IsFailed.Should().BeTrue();
        crunchyrollIdResult.Errors.First().Message.Should().Be(error);

        await _repository
            .Received(1)
            .GetSeasonIdByNameAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], folderName, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetSeasonIdByNumberAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], season.IndexNumber!.Value, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
                
        await _repository
            .Received(1)
            .GetAllSeasonsAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
}