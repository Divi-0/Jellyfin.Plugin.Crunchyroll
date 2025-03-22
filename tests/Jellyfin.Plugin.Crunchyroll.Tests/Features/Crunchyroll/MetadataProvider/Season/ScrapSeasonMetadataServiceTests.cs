using System.Globalization;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Season;

public class ScrapSeasonMetadataServiceTests
{
    private readonly IScrapSeasonMetadataRepository _repository;
    private readonly ICrunchyrollSeasonsClient _client;
    private readonly ILoginService _loginService;
    private readonly IScrapLockRepository _scrapLockRepository;
    private readonly TimeProvider _timeProvider;
    private readonly PluginConfiguration _config;
    
    private readonly ScrapSeasonMetadataService _sut;

    private readonly Fixture _fixture;
    private readonly Faker _faker;

    public ScrapSeasonMetadataServiceTests()
    {
        _repository = Substitute.For<IScrapSeasonMetadataRepository>();
        _client = Substitute.For<ICrunchyrollSeasonsClient>();
        _loginService = Substitute.For<ILoginService>();
        _scrapLockRepository = Substitute.For<IScrapLockRepository>();
        var logger = Substitute.For<ILogger<ScrapSeasonMetadataService>>();
        _timeProvider = Substitute.For<TimeProvider>();
        _config = new PluginConfiguration
        {
            CrunchyrollUpdateThresholdInDays = 0
        };
        
        _sut = new ScrapSeasonMetadataService(logger, _repository, _client, _loginService, _scrapLockRepository,
            _timeProvider, _config);

        _fixture = new Fixture();
        _faker = new Faker();
        
        _timeProvider
            .GetUtcNow()
            .Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SetsSeasons_WhenSeasonsFound_GivenSeriesId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);

        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        var expectedSeasons = new List<Domain.Entities.Season>();
        for (var index = 0; index < 5; index++)
        {
            var seasonResponseItem = _fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, CrunchyrollIdFaker.Generate().ToString)
                .Create();
            
            crunchyrollSeasonsItems.Add(seasonResponseItem);
            
            expectedSeasons.Add(new Domain.Entities.Season()
            {
                CrunchyrollId = seasonResponseItem.Id,
                Language = language.Name,
                Title = seasonResponseItem.Title,
                SlugTitle = seasonResponseItem.SlugTitle,
                Identifier = seasonResponseItem.Identifier,
                SeasonNumber = seasonResponseItem.SeasonNumber,
                SeasonSequenceNumber = seasonResponseItem.SeasonSequenceNumber,
                SeasonDisplayNumber = seasonResponseItem.SeasonDisplayNumber,
                SeriesId = titleMetadata.Id
            });
        }
        
        _client
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems}));
        
        Domain.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .UpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x => actualMetadata = x));
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        actualMetadata.Seasons.Should().BeEquivalentTo(expectedSeasons);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .Received(1)
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        _repository
            .Received(1)
            .UpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndAddsNewerSeasonsToSeasonsList_WhenNewSeasonsWereListed_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Domain.Entities.Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany(3)
            .ToList();
        
        titleMetadata.Seasons.AddRange(seasons);
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        foreach (var season in titleMetadata.Seasons)
        {
            crunchyrollSeasonsItems.Add(_fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, season.CrunchyrollId)
                .Create());
        }

        var newSeason = _fixture.Create<CrunchyrollSeasonsItem>();
        crunchyrollSeasonsItems.Add(newSeason);
        
        _client
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems}));
        
        Domain.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .UpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x => actualMetadata = x));
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .Received(1)
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualMetadata.Seasons.Should().HaveCount(4);

        foreach (var seasonsItem in crunchyrollSeasonsItems)
        {
            actualMetadata.Seasons.Should().Contain(x => x.CrunchyrollId == seasonsItem.Id);
        }

        _repository
            .Received(1)
            .UpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotUpdateSeasons_WhenLastUpdatedAtThresholdIsNotReached_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _config.CrunchyrollUpdateThresholdInDays = 999;

        var lastUpdatedAtDate = _faker.Date.Past().ToUniversalTime();
        var currentDateTime = _faker.Date.Future().ToUniversalTime();

        _timeProvider.GetUtcNow().Returns(currentDateTime);
        titleMetadata.LastUpdatedAt = lastUpdatedAtDate;
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .DidNotReceive()
            .GetSeasonsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        _repository
            .DidNotReceive()
            .UpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndDoesNotDeleteSeasons_WhenOldSeasonsWereRemoved_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        var seasons = _fixture.Build<Domain.Entities.Season>()
            .Without(x => x.Episodes)
            .Without(x => x.Series)
            .CreateMany(3)
            .ToList();
        
        titleMetadata.Seasons.AddRange(seasons);
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        _client
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = Array.Empty<CrunchyrollSeasonsItem>()});
        
        Domain.Entities.TitleMetadata actualMetadata = null!;
        _repository.UpdateTitleMetadata(
            Arg.Do<Domain.Entities.TitleMetadata>(x => actualMetadata = x));
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .Received(1)
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        actualMetadata.Seasons.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenLoginServiceFails_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var error = Guid.NewGuid().ToString();
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .DidNotReceive()
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetTitleMetadataFails_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .DidNotReceive()
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetTitleMetadataIsNull_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.NotFound);

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .DidNotReceive()
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenClientGetSeasonsFails_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var error = Guid.NewGuid().ToString();
        _client
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());
        
        await _client
            .Received(1)
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositorySaveChangesFails_GivenTitleId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        _client
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new CrunchyrollSeasonsResponse{Data = Array.Empty<CrunchyrollSeasonsItem>()});

        var error = Guid.NewGuid().ToString();
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());
        
        await _client
            .Received(1)
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());
        
        _repository
            .Received(1)
            .UpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetsSeasonsOnlyOnce_WhenTwoTasksEnterScrapService_GivenSeriesId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        var cancellationTokenSourceStartSecondTask = new CancellationTokenSource();
        var cancellationTokenSourceSecondTaskHasStarted = new CancellationTokenSource();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .With(x => x.LastUpdatedAt, _faker.Date.Past().ToUniversalTime)
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.Run(async () =>
            {
                await cancellationTokenSourceStartSecondTask.CancelAsync();

                while (!cancellationTokenSourceSecondTaskHasStarted.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }

                return Result.Ok<Domain.Entities.TitleMetadata?>(titleMetadata);
            }));

        var crunchyrollSeasonsItems = new List<CrunchyrollSeasonsItem>();
        var expectedSeasons = new List<Domain.Entities.Season>();
        for (var index = 0; index < 5; index++)
        {
            var seasonResponseItem = _fixture
                .Build<CrunchyrollSeasonsItem>()
                .With(x => x.Id, CrunchyrollIdFaker.Generate().ToString)
                .Create();
            
            crunchyrollSeasonsItems.Add(seasonResponseItem);
            
            expectedSeasons.Add(new Domain.Entities.Season()
            {
                CrunchyrollId = seasonResponseItem.Id,
                Language = language.Name,
                Title = seasonResponseItem.Title,
                SlugTitle = seasonResponseItem.SlugTitle,
                Identifier = seasonResponseItem.Identifier,
                SeasonNumber = seasonResponseItem.SeasonNumber,
                SeasonSequenceNumber = seasonResponseItem.SeasonSequenceNumber,
                SeasonDisplayNumber = seasonResponseItem.SeasonDisplayNumber,
                SeriesId = titleMetadata.Id
            });
        }
        
        _client
            .GetSeasonsAsync(seriesId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new CrunchyrollSeasonsResponse{Data = crunchyrollSeasonsItems}));
        
        Domain.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .UpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x => actualMetadata = x));
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(true);
        
        //Act
        var firstTask = _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);
        var secondTask = Task.Run(async () =>
        {
            while (!cancellationTokenSourceStartSecondTask.IsCancellationRequested)
            {
                await Task.Delay(500);
            }

            await cancellationTokenSourceSecondTaskHasStarted.CancelAsync();
            return await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);
        });

        var results = await Task.WhenAll(firstTask, secondTask);

        //Assert
        results[0].IsSuccess.Should().BeTrue();
        results[1].IsSuccess.Should().BeTrue();
        
        actualMetadata.Seasons.Should().BeEquivalentTo(expectedSeasons);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _client
            .Received(1)
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        _repository
            .Received(1)
            .UpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SkipsScraping_WhenLockReturnsFalse_GivenSeriesId()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        _scrapLockRepository
            .AddLockAsync(seriesId)
            .Returns(false);
        
        //Act
        var result = await _sut.ScrapSeasonMetadataAsync(seriesId, language, CancellationToken.None);

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
            .GetSeasonsAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        _repository
            .DidNotReceive()
            .UpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}