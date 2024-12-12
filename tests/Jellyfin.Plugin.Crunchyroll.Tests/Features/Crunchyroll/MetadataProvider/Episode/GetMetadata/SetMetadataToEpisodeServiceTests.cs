using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;

public class SetMetadataToEpisodeServiceTests
{
    private readonly SetMetadataToEpisodeService _sut;
    private readonly ISetMetadataToEpisodeRepository _repository;
    private readonly PluginConfiguration _config;

    public SetMetadataToEpisodeServiceTests()
    {
        _repository = Substitute.For<ISetMetadataToEpisodeRepository>();
        var logger = Substitute.For<ILogger<SetMetadataToEpisodeService>>();
        _config = new PluginConfiguration
        {
            IsFeatureEpisodeTitleEnabled = true,
            IsFeatureEpisodeDescriptionEnabled = true,
            IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = true
        };
        _sut = new SetMetadataToEpisodeService(_repository, logger, _config);
    }
    
    [Fact]
    public async Task SetsTitleAndDescription_WhenSuccessful_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        episodeWithNewMetadata.Name.Should().Be(crunchyrollEpisode.Title);
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber!.HasValue.Should().BeFalse();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, language, 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsTitleNameWithoutBrackets_WhenTitleHasBracketsAtStart_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var expectedTitle = new string(crunchyrollEpisode.Title);
        crunchyrollEpisode = crunchyrollEpisode with { Title = $"(OMU) {crunchyrollEpisode.Title}" };
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        episodeWithNewMetadata.Name.Should().Be(expectedTitle);
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber!.HasValue.Should().BeFalse();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, language, 
                Arg.Any<CancellationToken>());
    }
    
    [Theory]
    [InlineData("", 0)]
    [InlineData("432", 432)]
    [InlineData("FMI1", 1)]
    [InlineData("FMI2", 2)]
    public async Task SetsIndexNumberAndTitleWithEpisodeNumber_WhenIndexNumberOfJellyfinEpisodeIsNull_GivenEpisodeId(
        string episodeIdentifier, int? expectedIndexNumber)
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)null;
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        if (!string.IsNullOrWhiteSpace(episodeIdentifier))
        {
            crunchyrollEpisode = crunchyrollEpisode with
            {
                EpisodeNumber = episodeIdentifier,
                SequenceNumber = Convert.ToDouble(expectedIndexNumber)
            };
        }
        
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        episodeWithNewMetadata.Name.Should().Be($"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}");
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber!.Should().Be(expectedIndexNumber == 0 
            ? int.Parse(crunchyrollEpisode.EpisodeNumber) 
            : expectedIndexNumber);
        episodeWithNewMetadata.AirsBeforeEpisodeNumber.Should().BeNull();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, language, 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsNameAndDescription_WhenIndexNumberOfJellyfinEpisodeIsNullAndCrunchyrollEpisodeNumberIsEmpty_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)null;
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        crunchyrollEpisode = crunchyrollEpisode with { EpisodeNumber = string.Empty };

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        episodeWithNewMetadata.Name.Should().Be(crunchyrollEpisode.Title);
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber.Should().BeNull();
        episodeWithNewMetadata.AirsBeforeEpisodeNumber.Should().BeNull();
    }
    
    [Fact]
    public async Task SetsAirsBefore_WhenIndexNumberOfJellyfinEpisodeIsNullAndCrunchyrollEpisodeNumberIsDecimal_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollSeason = CrunchyrollSeasonFaker.Generate();
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate(crunchyrollSeason.Id);
        crunchyrollEpisode = crunchyrollEpisode with { Season = crunchyrollSeason };
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)null;
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        crunchyrollEpisode = crunchyrollEpisode with
        {
            SequenceNumber = Random.Shared.Next(1, int.MaxValue - 1) + 0.5
        };

        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        episodeWithNewMetadata.Name.Should().Be($"{crunchyrollEpisode.EpisodeNumber} - {crunchyrollEpisode.Title}");
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber!.Should().Be(null);
        episodeWithNewMetadata.AirsBeforeEpisodeNumber.Should().Be(Convert.ToInt32(crunchyrollEpisode.SequenceNumber + 0.5));
        episodeWithNewMetadata.AirsBeforeSeasonNumber.Should().Be(crunchyrollSeason.SeasonNumber);
        episodeWithNewMetadata.ParentIndexNumber.HasValue.Should().BeFalse();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenGetEpisodeFails_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)null;
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(error);
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetEpisodeReturnsNull_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)null;
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Episode?>(null));

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(ErrorCodes.NotFound);
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateTitle_WhenFeatureEpisodeTitleIsDisabled_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        _config.IsFeatureEpisodeTitleEnabled = false;
        
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        
        episodeWithNewMetadata.Name.Should().NotBe(crunchyrollEpisode.Title, "feature is disabled");
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber!.HasValue.Should().BeFalse();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, language, 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateOverview_WhenFeatureEpisodeDescriptionIsDisabled_GivenEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        _config.IsFeatureEpisodeDescriptionEnabled = false;
        
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        
        episodeWithNewMetadata.Name.Should().Be(crunchyrollEpisode.Title);
        episodeWithNewMetadata.Overview.Should().NotBe(crunchyrollEpisode.Description, "feature is disabled");
        episodeWithNewMetadata.IndexNumber.HasValue.Should().BeFalse();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotSetIndexNumberAndTitleWithEpisodeNumber_WhenIndexNumberOfJellyfinEpisodeIsNullAndFeatureEpisodeIncludeSpecialsInNormalSeasonsIsDisabled_GivenEpisodeWithEpisodeId()
    {
        //Arrange
        var crunchyrollEpisode = CrunchyrollEpisodeFaker.Generate();
        var language = new CultureInfo("en-US");
        var currentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        var parentIndexNumber = (int?)Random.Shared.Next(1, int.MaxValue);
        
        _config.IsFeatureEpisodeIncludeSpecialsInNormalSeasonsEnabled = false;
        
        const string episodeIdentifier = "FMI3";
        const int expectedIndexNumber = 3;
        crunchyrollEpisode = crunchyrollEpisode with
        {
            EpisodeNumber = episodeIdentifier,
            SequenceNumber = Convert.ToDouble(expectedIndexNumber)
        };
        
        _repository
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisode);

        //Act
        var setMetadataResult = await _sut.SetMetadataToEpisodeAsync(crunchyrollEpisode.CrunchyrollId, 
            currentIndexNumber, parentIndexNumber, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var episodeWithNewMetadata = setMetadataResult.Value;
        
        episodeWithNewMetadata.Name.Should().Be(crunchyrollEpisode.Title);
        episodeWithNewMetadata.Overview.Should().Be(crunchyrollEpisode.Description);
        episodeWithNewMetadata.IndexNumber!.Should().NotBe(expectedIndexNumber);
        episodeWithNewMetadata.AirsBeforeEpisodeNumber.Should().BeNull();
        
        await _repository
            .Received(1)
            .GetEpisodeAsync(crunchyrollEpisode.CrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
}