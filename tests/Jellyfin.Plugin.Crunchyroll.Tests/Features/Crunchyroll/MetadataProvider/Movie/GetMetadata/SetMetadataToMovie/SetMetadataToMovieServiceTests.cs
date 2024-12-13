using System.Globalization;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;

public class SetMetadataToMovieServiceTests
{
    private readonly SetMetadataToMovieService _sut;
    private readonly ISetMetadataToMovieRepository _repository;
    private readonly PluginConfiguration _config;

    public SetMetadataToMovieServiceTests()
    {
        _repository = Substitute.For<ISetMetadataToMovieRepository>();
        var logger = Substitute.For<ILogger<SetMetadataToMovieService>>();
        _config = new PluginConfiguration
        {
            IsFeatureMovieTitleEnabled = true,
            IsFeatureMovieDescriptionEnabled = true,
            IsFeatureMovieStudioEnabled = true
        };
        _sut = new SetMetadataToMovieService(_repository, logger, _config);
    }
    
    [Fact]
    public async Task SetsMetadata_WhenSuccessful_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var movie = setMetadataResult.Value;
        
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsTitleNameWithoutBrackets_WhenTitleHasBracketsAtStart_GivenMovieWithIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        var expectedTitle = new string(episode.Title);
        episode = episode with
        {
            Title = $"(OMU) {episode.Title}"
        };
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var movie = setMetadataResult.Value;
        
        movie.Name.Should().Be(expectedTitle);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetTitlemetadataFailed_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(error);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetTitlemetadataReturnedNull_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(ErrorCodes.NotFound);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateTitle_WhenFeatureMovieTitleIsDisabled_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        _config.IsFeatureMovieTitleEnabled = false;

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var movie = setMetadataResult.Value;
        
        movie.Name.Should().NotBe(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateOverview_WhenFeatureMovieDescriptionIsDisabled_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        _config.IsFeatureMovieDescriptionEnabled = false;

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var movie = setMetadataResult.Value;
        
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().NotBe(episode.Description);
        movie.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateStudios_WhenFeatureMovieStudioIsDisabled_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episode = CrunchyrollEpisodeFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        season.Episodes.Add(episode);
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        _config.IsFeatureMovieStudioEnabled = false;

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episode.CrunchyrollId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsSuccess.Should().BeTrue();
        var movie = setMetadataResult.Value;
        
        movie.Name.Should().Be(episode.Title);
        movie.Overview.Should().Be(episode.Description);
        movie.Studios.Should().NotContain(titleMetadata.Studio);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenMovieEpisodeCanNotBeFoundInTitleMetadata_GivenIds()
    {
        //Arrange
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        var season = CrunchyrollSeasonFaker.Generate();
        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        //Act
        var setMetadataResult = await _sut.SetMetadataToMovieAsync(titleMetadata.CrunchyrollId, season.CrunchyrollId, 
            episodeId, language, CancellationToken.None);

        //Assert
        setMetadataResult.IsFailed.Should().BeTrue();
        setMetadataResult.Errors.First().Message.Should().Be(ErrorCodes.NotFound);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(titleMetadata.CrunchyrollId, language, Arg.Any<CancellationToken>());
    }
}