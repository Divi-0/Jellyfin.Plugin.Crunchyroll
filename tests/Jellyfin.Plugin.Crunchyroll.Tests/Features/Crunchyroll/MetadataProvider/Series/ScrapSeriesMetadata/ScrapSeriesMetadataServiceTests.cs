using System.Globalization;
using System.Text.Json;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series.Dtos;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;

public class ScrapSeriesMetadataServiceTests
{
    private readonly IScrapSeriesMetadataRepository _repository;
    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSeriesClient _crunchyrollSeriesClient;
    
    private readonly ScrapSeriesMetadataService _sut;

    private readonly Faker _faker;
    private readonly Fixture _fixture;

    public ScrapSeriesMetadataServiceTests()
    {
        _repository = Substitute.For<IScrapSeriesMetadataRepository>();
        _loginService = Substitute.For<ILoginService>();
        _crunchyrollSeriesClient = Substitute.For<ICrunchyrollSeriesClient>();
        
        _sut = new ScrapSeriesMetadataService(_repository, _loginService, _crunchyrollSeriesClient);

        _faker = new Faker();
        _fixture = new Fixture();
    }
    
    [Fact]
    public async Task ReturnsSuccessAndStoresMetadata_WhenCalled_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = seriesCrunchyrollId,
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
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);

        var rating = 3.5f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
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
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = seriesCrunchyrollId,
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
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
        [Fact]
    public async Task SetsTitleMetadataRatingToZero_WhenGetSeriesRatingFailed_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = seriesCrunchyrollId,
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
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
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
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        var expectedTitleMetadata = new Domain.Entities.TitleMetadata()
        {
            CrunchyrollId = seriesCrunchyrollId,
            Title = seriesMetadataResponse.Title,
            Description = seriesMetadataResponse.Description,
            SlugTitle = seriesMetadataResponse.SlugTitle,
            Studio = seriesMetadataResponse.ContentProvider,
            Rating = 0,
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
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetTitleMetadataFails_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeriesClient
            .DidNotReceive()
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenSeriesMetadataRequestFailed_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<Domain.Entities.TitleMetadata?>(null));

        var error = Guid.NewGuid().ToString();
        _crunchyrollSeriesClient
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        await _repository
            .DidNotReceive()
            .AddOrUpdateTitleMetadata(Arg.Any<Domain.Entities.TitleMetadata>(),
                Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenLoginFails_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsSuccessAndUpdateMetadataOfSeries_WhenNewMetadataForSeriesIsAvailable_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var titleMetadata = _fixture.Build<Domain.Entities.TitleMetadata>()
            .Without(x => x.Seasons)
            .Create();
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = seriesCrunchyrollId,
            Title = _faker.Random.Word(),
            SlugTitle = _faker.Random.Word(),
            Description = _faker.Lorem.Sentences(),
            ContentProvider = _faker.Company.CompanyName(),
            Images = new CrunchyrollSeriesImageItem
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
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        const float rating = 1.1f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        Domain.Entities.TitleMetadata actualMetadata = null!;
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Do<Domain.Entities.TitleMetadata>(x => actualMetadata = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        actualMetadata.CrunchyrollId.Should().Be(titleMetadata.CrunchyrollId);
        actualMetadata.SlugTitle.Should().Be(titleMetadata.SlugTitle);
        actualMetadata.Title.Should().Be(seriesMetadataResponse.Title);
        actualMetadata.Description.Should().Be(seriesMetadataResponse.Description);
        actualMetadata.Studio.Should().Be(seriesMetadataResponse.ContentProvider);
        actualMetadata.Rating.Should().Be(rating);
        var posterTall = JsonSerializer.Deserialize<ImageSource>(actualMetadata.PosterTall)!;
        var posterWide = JsonSerializer.Deserialize<ImageSource>(actualMetadata.PosterWide)!;
        posterTall.Uri.Should().Be(seriesMetadataResponse.Images.PosterTall.First().Last().Source);
        posterWide.Uri.Should().Be(seriesMetadataResponse.Images.PosterWide.First().Last().Source);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryAddOrUpdateFails_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = seriesCrunchyrollId,
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
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        var rating = 2.5f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
            .Returns(rating);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<Domain.Entities.TitleMetadata?>(null)));
        
        _repository
            .AddOrUpdateTitleMetadata(
                Arg.Any<Domain.Entities.TitleMetadata>(), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        //Act
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositorySaveChangesFails_GivenTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var seriesMetadataResponse = new CrunchyrollSeriesContentItem
        {
            Id = seriesCrunchyrollId,
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
            .GetSeriesMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(seriesMetadataResponse);
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<Domain.Entities.TitleMetadata?>(null)));
        
        var rating = 1.2f;
        _crunchyrollSeriesClient
            .GetRatingAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CancellationToken>())
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
        var result = await _sut.ScrapSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollSeriesClient
            .Received(1)
            .GetSeriesMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}