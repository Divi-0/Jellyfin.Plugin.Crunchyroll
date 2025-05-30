using System.Globalization;
using System.Text.Json;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public class ScrapEpisodeMetadataServiceTests
{
    private readonly ScrapEpisodeMetadataService _sut;
    
    private readonly IScrapEpisodeCrunchyrollClient _client;
    private readonly ILoginService _loginService;
    private readonly IScrapEpisodeMetadataRepository _repository;
    private readonly IScrapLockRepository _scrapLockRepository;
    private readonly IScrapMissingEpisodeService _scrapMissingEpisodeService;
    private readonly TimeProvider _timeProvider;
    private readonly PluginConfiguration _config;

    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    public ScrapEpisodeMetadataServiceTests()
    {
        _client = Substitute.For<IScrapEpisodeCrunchyrollClient>();
        _loginService = Substitute.For<ILoginService>();
        _repository = Substitute.For<IScrapEpisodeMetadataRepository>();
        _scrapLockRepository = Substitute.For<IScrapLockRepository>();
        var logger = Substitute.For<ILogger<ScrapEpisodeMetadataService>>();
        _scrapMissingEpisodeService = Substitute.For<IScrapMissingEpisodeService>();
        _timeProvider = Substitute.For<TimeProvider>();
        _config = new PluginConfiguration
        {
            CrunchyrollUpdateThresholdInDays = 0
        };
        
        _sut = new ScrapEpisodeMetadataService(_client, _loginService, _repository, logger, _scrapLockRepository,
            _scrapMissingEpisodeService, _timeProvider, _config);

        _fixture = new Fixture();
        _faker = new Faker();

        _timeProvider
            .GetUtcNow()
            .Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ReturnsSuccessAndStoresEpisodes_WhenSuccessful_GivenNoStoredEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var crunchyrollEpisodes = _fixture.Create<CrunchyrollEpisodesResponse>();
        _client
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisodes);

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(season);

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);
        
        _scrapMissingEpisodeService
            .ScrapMissingEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        season.Episodes.Should().BeEquivalentTo(crunchyrollEpisodes.Data.Select(x => new Domain.Entities.Episode
        {
            CrunchyrollId = x.Id,
            Title = x.Title,
            Description = x.Description,
            EpisodeNumber = x.Episode,
            SequenceNumber = x.SequenceNumber,
            Language = language.Name,
            SlugTitle = x.SlugTitle,
            SeasonId = season.Id,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = x.Images.Thumbnail.First().Last().Source,
                Height = x.Images.Thumbnail.First().Last().Height,
                Width = x.Images.Thumbnail.First().Last().Width,
            })
        }));

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .Received(1)
            .GetEpisodesAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        _repository
            .Received(1)
            .UpdateSeason(season);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotUpdateEpisodes_WhenLastUpdatedAtThresholdIsNotReached_GivenNoStoredEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();

        _config.CrunchyrollUpdateThresholdInDays = 999;

        var lastUpdatedAtDate = _faker.Date.Past().ToUniversalTime();
        var currentDateTime = _faker.Date.Future().ToUniversalTime();

        _timeProvider.GetUtcNow().Returns(currentDateTime);
        season.LastUpdatedAt = lastUpdatedAtDate;
        
        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(season);

        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);

        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .DidNotReceive()
            .GetEpisodesAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        _repository
            .DidNotReceive()
            .UpdateSeason(season);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetSeasonFails_GivenNoStoredEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        var error = Guid.NewGuid().ToString();
        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .DidNotReceive()
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenLoginFails_GivenNoStoredEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(season);

        var error = Guid.NewGuid().ToString();
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .DidNotReceive()
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenClientEpisodeRequestFailed_GivenSeasonId()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(season);
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var error = Guid.NewGuid().ToString();
        _client
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .Received(1)
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerEpisodesToEpisodesList_WhenCompletelyNewEpisodesWereListedAndOldRemoved_GivenExistingEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();

        var existingEpisodes = Enumerable.Range(1, 10)
            .Select(_ => CrunchyrollEpisodeFaker.Generate())
            .ToList();
        season.Episodes.AddRange(existingEpisodes);
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var crunchyrollEpisodes = _fixture.Create<CrunchyrollEpisodesResponse>();
        _client
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisodes);

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(season);

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);

        _scrapMissingEpisodeService
            .ScrapMissingEpisodeAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        var newEpisodes = crunchyrollEpisodes.Data.Select(x => new Domain.Entities.Episode
        {
            CrunchyrollId = x.Id,
            Title = x.Title,
            Description = x.Description,
            EpisodeNumber = x.Episode,
            SequenceNumber = x.SequenceNumber,
            Language = language.Name,
            SlugTitle = x.SlugTitle,
            SeasonId = season.Id,
            Thumbnail = JsonSerializer.Serialize(new ImageSource
            {
                Uri = x.Images.Thumbnail.First().Last().Source,
                Height = x.Images.Thumbnail.First().Last().Height,
                Width = x.Images.Thumbnail.First().Last().Width,
            })
        });
        
        season.Episodes.Should().Contain(existingEpisodes);
        season.Episodes.Should().Contain(newEpisodes);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .Received(1)
            .GetEpisodesAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        _repository
            .Received(1)
            .UpdateSeason(season);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositorySaveChangesFails_GivenNoStoredEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var crunchyrollEpisodes = _fixture.Create<CrunchyrollEpisodesResponse>();
        _client
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisodes);

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(season);

        var error = Guid.NewGuid().ToString();
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);

        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .Received(1)
            .GetEpisodesAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        _repository
            .Received(1)
            .UpdateSeason(season);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetSeasonReturnsNull_GivenNoStoredSeason()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var crunchyrollEpisodes = _fixture.Create<CrunchyrollEpisodesResponse>();
        _client
            .GetEpisodesAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(crunchyrollEpisodes);

        _repository
            .GetSeasonAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.Season?>(null));
        
        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(true);

        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.NotFound);

        await _repository
            .Received(1)
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .DidNotReceive()
            .GetEpisodesAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsScrapping_WhenScrapLockReturnsFalse_GivenNoStoredEpisodes()
    {
        //Arrange
        var season = CrunchyrollSeasonFaker.Generate();
        var language = new CultureInfo("en-US");
        var episodeId = CrunchyrollIdFaker.Generate();

        _scrapLockRepository
            .AddLockAsync(season.CrunchyrollId)
            .Returns(false);

        //Act
        var result = await _sut.ScrapEpisodeMetadataAsync(season.CrunchyrollId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _scrapLockRepository
            .Received(1)
            .AddLockAsync(Arg.Any<CrunchyrollId>());

        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _client
            .DidNotReceive()
            .GetEpisodesAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .GetSeasonAsync(season.CrunchyrollId, language, Arg.Any<CancellationToken>());

        _repository
            .DidNotReceive()
            .UpdateSeason(season);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}