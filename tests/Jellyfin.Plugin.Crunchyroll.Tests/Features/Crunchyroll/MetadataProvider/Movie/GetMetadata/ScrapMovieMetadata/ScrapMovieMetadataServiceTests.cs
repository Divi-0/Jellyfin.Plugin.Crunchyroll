using System.Globalization;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;

public class ScrapMovieMetadataServiceTests
{
    private readonly ScrapMovieMetadataService _sut;
    private readonly IScrapSeriesMetadataService _scrapSeriesMetadataService;
    private readonly IScrapSeasonMetadataService _scrapSeasonMetadataService;
    private readonly IScrapEpisodeMetadataService _scrapEpisodeMetadataService;
    private readonly IScrapMovieMetadataRepository _repository;
    private readonly IScrapEpisodeCrunchyrollClient _crunchyrollEpisodesClient;

    private readonly Faker _faker;

    public ScrapMovieMetadataServiceTests()
    {
        _scrapSeriesMetadataService = Substitute.For<IScrapSeriesMetadataService>();
        _scrapSeasonMetadataService = Substitute.For<IScrapSeasonMetadataService>();
        _scrapEpisodeMetadataService = Substitute.For<IScrapEpisodeMetadataService>();
        _repository = Substitute.For<IScrapMovieMetadataRepository>();
        _crunchyrollEpisodesClient = Substitute.For<IScrapEpisodeCrunchyrollClient>();
        var logger = Substitute.For<ILogger<ScrapMovieMetadataService>>();
        _sut = new ScrapMovieMetadataService(_scrapSeriesMetadataService, _scrapSeasonMetadataService, 
            _scrapEpisodeMetadataService, _repository, _crunchyrollEpisodesClient, logger);

        _faker = new Faker();
    }

    [Fact]
    public async Task ReturnsFailed_WhenScrapSeriesMetadataFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        var error = Guid.NewGuid().ToString();
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .DidNotReceive()
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .DidNotReceive()
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenScrapSeasonMetadataFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .DidNotReceive()
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenScrapEpisodeMetadataFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var error = Guid.NewGuid().ToString();
        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ScrapsExtraEpisodeIdForMovie_WhenMovieEpisodeIdNotPresentInMetadata_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = season.CrunchyrollId;
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = episodeId,
            Title = _faker.Random.Words(Random.Shared.Next(1, 10)),
            Description = _faker.Lorem.Sentences(Random.Shared.Next(1, 10)),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0,
                SeriesTitle = string.Empty,
                SeasonSlugTitle = string.Empty
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);
        
        _repository
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        titleMetadata.Seasons.SelectMany(x => x.Episodes).Should().Contain(x => x.CrunchyrollId == episodeId);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadataAsync(titleMetadata, Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ScrapsExtraEpisodeIdForMovie_WhenMovieEpisodeIdNotPresentInMetadataAndSeasonAlsoNotFound_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var seasonId = CrunchyrollIdFaker.Generate();
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate();
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = episodeId,
            Title = _faker.Random.Words(Random.Shared.Next(1, 10)),
            Description = _faker.Lorem.Sentences(Random.Shared.Next(1, 10)),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0,
                SeriesTitle = string.Empty,
                SeasonSlugTitle = string.Empty
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);
        
        _repository
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        titleMetadata.Seasons.Should().Contain(x => x.CrunchyrollId == seasonId);
        titleMetadata.Seasons.SelectMany(x => x.Episodes).Should().Contain(x => x.CrunchyrollId == episodeId);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadataAsync(titleMetadata, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ScrapsNotExtraEpisodeIdForMovie_WhenMovieEpisodeIdAlreadyPresentInMetadata_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        var season = CrunchyrollSeasonFaker.Generate();
        var episode = CrunchyrollEpisodeFaker.Generate();
        season.Episodes.AddRange([episode]);
        
        var seasonId = season.CrunchyrollId;
        var episodeId = episode.CrunchyrollId;

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        titleMetadata.Seasons.Should().Contain(x => x.CrunchyrollId == seasonId);
        titleMetadata.Seasons.SelectMany(x => x.Episodes).Should().Contain(x => x.CrunchyrollId == episodeId);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadataAsync(titleMetadata, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenMovieEpisodeIdNotPresentInMetadataAndRepositoryGetMetadataFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = season.CrunchyrollId;
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenMovieEpisodeIdNotPresentInMetadataAndRepositoryGetMetadataReturnedNull_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = season.CrunchyrollId;
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.NotFound);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenMovieEpisodeIdNotPresentInMetadataAndEpisodeClientFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = new CrunchyrollId(season.CrunchyrollId);
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);

        var error = Guid.NewGuid().ToString();
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenMovieEpisodeIdNotPresentInMetadataAndRepositoryAddOrUpdateFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = season.CrunchyrollId;
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = episodeId,
            Title = _faker.Random.Words(Random.Shared.Next(1, 10)),
            Description = _faker.Lorem.Sentences(Random.Shared.Next(1, 10)),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0,
                SeriesTitle = string.Empty,
                SeasonSlugTitle = string.Empty
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);

        var error = Guid.NewGuid().ToString();
        _repository
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenMovieEpisodeIdNotPresentInMetadataAndRepositorySaveChangesFailed_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = season.CrunchyrollId;
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = episodeId,
            Title = _faker.Random.Words(Random.Shared.Next(1, 10)),
            Description = _faker.Lorem.Sentences(Random.Shared.Next(1, 10)),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0,
                SeriesTitle = string.Empty,
                SeasonSlugTitle = string.Empty
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);
        
        _repository
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ScrapsExtraEpisodeIdForMovie_WhenMovieEpisodeIdNotPresentInMetadataAndScrapEpisodesFailedWithRequestFailedErrorCode_GivenValidIds()
    {
        //Arrange
        var seriesId = CrunchyrollIdFaker.Generate();
        var season = CrunchyrollSeasonFaker.Generate();
        var seasonId = season.CrunchyrollId;
        var episodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _scrapSeriesMetadataService
            .ScrapSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapSeasonMetadataService
            .ScrapSeasonMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _scrapEpisodeMetadataService
            .ScrapEpisodeMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CrunchyrollId?>(),Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(EpisodesErrorCodes.RequestFailed));

        var titleMetadata = CrunchyrollTitleMetadataFaker.Generate(seasons: [season]);
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var episodeResponse = new CrunchyrollEpisodeDataItem
        {
            Id = episodeId,
            Title = _faker.Random.Words(Random.Shared.Next(1, 10)),
            Description = _faker.Lorem.Sentences(Random.Shared.Next(1, 10)),
            Images = new CrunchyrollEpisodeImages()
            {
                Thumbnail = [[new CrunchyrollEpisodeThumbnailSizes()
                {
                    Type = "Thumbnail",
                    Height = 10,
                    Width = 43,
                    Source = _faker.Internet.UrlWithPath(fileExt: "png")
                }]]
            },
            EpisodeMetadata = new CrunchyrollEpisodeDataItemEpisodeMetadata
            {
                Episode = string.Empty,
                EpisodeNumber = null,
                SeasonId = CrunchyrollIdFaker.Generate(),
                SequenceNumber = 0,
                SeriesId = CrunchyrollIdFaker.Generate(),
                SeriesSlugTitle = string.Empty,
                SeasonNumber = 0,
                SeasonTitle = string.Empty,
                SeasonDisplayNumber = string.Empty,
                SeasonSequenceNumber = 0,
                SeriesTitle = string.Empty,
                SeasonSlugTitle = string.Empty
            }
        };
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(episodeResponse);
        
        _repository
            .AddOrUpdateTitleMetadataAsync(Arg.Any<Domain.Entities.TitleMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapMovieMetadataAsync(seriesId, seasonId, episodeId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        titleMetadata.Seasons.SelectMany(x => x.Episodes).Should().Contain(x => x.CrunchyrollId == episodeId);
        
        await _scrapSeriesMetadataService
            .Received(1)
            .ScrapSeriesMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapSeasonMetadataService
            .Received(1)
            .ScrapSeasonMetadataAsync(seriesId, language,
                Arg.Any<CancellationToken>());

        await _scrapEpisodeMetadataService
            .Received(1)
            .ScrapEpisodeMetadataAsync(seasonId, null, language,
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesId, language, Arg.Any<CancellationToken>());

        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(episodeId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadataAsync(titleMetadata, Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}