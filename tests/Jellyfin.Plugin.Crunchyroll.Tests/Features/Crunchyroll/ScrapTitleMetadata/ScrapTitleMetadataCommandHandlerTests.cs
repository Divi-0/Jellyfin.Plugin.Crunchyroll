using System.Globalization;
using System.Text.Json;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ScrapTitleMetadata;

public class ScrapTitleMetadataCommandHandlerTests
{
    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    private readonly ScrapTitleMetadataCommandHandler _sut;
    
    private readonly IScrapTitleMetadataRepository _repository;
    private readonly ICrunchyrollSeasonsClient _crunchyrollSeasonsClient;
    private readonly ICrunchyrollEpisodesClient _crunchyrollEpisodesClient;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;

    public ScrapTitleMetadataCommandHandlerTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
        
        _repository = Substitute.For<IScrapTitleMetadataRepository>();
        _crunchyrollSeasonsClient = Substitute.For<ICrunchyrollSeasonsClient>();
        _crunchyrollEpisodesClient = Substitute.For<ICrunchyrollEpisodesClient>();
        _loginService = Substitute.For<ILoginService>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        _sut = new ScrapTitleMetadataCommandHandler(_repository, _crunchyrollSeasonsClient,
            _crunchyrollEpisodesClient, _loginService, _crunchyrollSeriesClient);
    }

    [Fact]
    public async Task ReturnsSuccessAndStoresMetadata_WhenCalled_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);

        var rating = 3.5f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        Domain.Entities.TitleMetadata actualTitleMetadata = null!;
         _repository
             .AddOrUpdateTitleMetadata(Arg.Do<Domain.Entities.TitleMetadata>(
                    x => actualTitleMetadata = x),
                Arg.Any<CancellationToken>())
             .Returns(Result.Ok());

         _repository
             .SaveChangesAsync(Arg.Any<CancellationToken>())
             .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US")};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = rating,
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterTall.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterTall.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterTall.First().Last().Height
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterWide.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterWide.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterWide.First().Last().Height
            }),
            Language = language.Name
        };
        
        actualTitleMetadata.Should().BeEquivalentTo(expectedTitleMetadata, opt => opt
            .Excluding(x => x.Id)
            .Excluding(x => x.Seasons));

        actualTitleMetadata.Id.Should().NotBeEmpty();
        
        actualTitleMetadata.Seasons.Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.CrunchyrollId);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                var thumbnail = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
                thumbnail.Uri.Should().NotBeNullOrEmpty();
                thumbnail.Width.Should().NotBe(0);
                thumbnail.Height.Should().NotBe(0);
            });
        });
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task StoresEmptyEpisodeList_WhenEpisodeRequestFailed_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));
        
        var error = _fixture.Create<string>();
        _crunchyrollEpisodesClient
            .GetEpisodesAsync(seasonsResponse.Data.First().Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        var seriesContentItem = _fixture.Create<CrunchyrollSeriesContentItem>();
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesContentItem);
        
        var rating = 0.1f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);

        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US") };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .AddOrUpdateTitleMetadata(Arg.Is<Domain.Entities.TitleMetadata>(x => 
                x.CrunchyrollId == titleId &&
                x.SlugTitle == seriesContentItem.SlugTitle &&
                x.Seasons.All(season => seasonsResponse.Data.Any(y => y.Id == season.CrunchyrollId)) &&
                x.Seasons.Count(season => season.Episodes.Count == 0) == 1),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotConatinExtraEpisodeIdForMovie_WhenGetEpisodeForMovieEpisodeIdFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var extraEpisodeId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 2.9f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        Domain.Entities.TitleMetadata actualTitleMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x =>
                    actualTitleMetadata = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        _crunchyrollEpisodesClient
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ScrapTitleMetadataCommand
        {
            TitleId = titleId, 
            MovieEpisodeId = extraEpisodeId,
            MovieSeasonId = seasonsResponse.Data.First().Id,
            Language = new CultureInfo("en-US")
        };
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = titleId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = rating,
            PosterTall = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterTall.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterTall.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterTall.First().Last().Height
            }),
            PosterWide = JsonSerializer.Serialize(new ImageSource
            {
                Uri = seriesMetadataResponse.Images.PosterWide.First().Last().Source,
                Width = seriesMetadataResponse.Images.PosterWide.First().Last().Width,
                Height = seriesMetadataResponse.Images.PosterWide.First().Last().Height
            }),
            Language = language.Name
        };
        
        actualTitleMetadata.Should().BeEquivalentTo(expectedTitleMetadata, opt => opt
            .Excluding(x => x.Id)
            .Excluding(x => x.Seasons));

        actualTitleMetadata.Id.Should().NotBeEmpty();
        
        actualTitleMetadata.Seasons.Should().AllSatisfy(season =>
        {
            seasonsResponse.Data.Should().Contain(y => y.Id == season.CrunchyrollId);
            season.Episodes.Should().NotBeEmpty();

            season.Episodes.Should().AllSatisfy(episode =>
            {
                var thumbnail = JsonSerializer.Deserialize<ImageSource>(episode.Thumbnail)!;
                thumbnail.Uri.Should().NotBeNullOrEmpty();
                thumbnail.Width.Should().NotBe(0);
                thumbnail.Height.Should().NotBe(0);
            });
        });
        
        await _crunchyrollEpisodesClient
            .Received(1)
            .GetEpisodeAsync(extraEpisodeId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        actualTitleMetadata.Seasons.Should().NotContain(x => x.Episodes.Any(y => y.CrunchyrollId == extraEpisodeId));
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryAddOrUpdateFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 2.5f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<Domain.Entities.TitleMetadata?>(null)));
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US")};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositorySaveChangesFails_GivenTitleId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var seasonsResponse = _fixture.Create<CrunchyrollSeasonsResponse>();
        _crunchyrollSeasonsClient
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seasonsResponse);
        
        foreach (var season in seasonsResponse.Data)
        {
            var episodesResponse = _fixture.Create<CrunchyrollEpisodesResponse>();
            _crunchyrollEpisodesClient
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(episodesResponse);
        }

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = titleId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem()
            {
                PosterTall = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_tall",
                    Height = 0,
                    Width = 0
                }]],
                PosterWide = [[new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                },new CrunchyrollSeriesImage
                {
                    Source = _faker.Internet.UrlWithPath(),
                    Type = "poster_wide",
                    Height = 0,
                    Width = 0
                }]],
            }
        };
        
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        _repository
            .GetTitleMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<Domain.Entities.TitleMetadata?>(null)));
        
        var rating = 1.2f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var command = new ScrapTitleMetadataCommand { TitleId = titleId, Language = new CultureInfo("en-US")};
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeasonsClient
            .Received(1)
            .GetSeasonsAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        foreach (var season in seasonsResponse.Data)
        {
            await _crunchyrollEpisodesClient
                .Received(1)
                .GetEpisodesAsync(season.Id, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        }

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
        
        await _crunchyrollEpisodesClient
            .DidNotReceive()
            .GetEpisodeAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}