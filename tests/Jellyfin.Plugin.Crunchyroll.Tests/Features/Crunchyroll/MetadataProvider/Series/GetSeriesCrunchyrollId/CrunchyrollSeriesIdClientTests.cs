using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using Bogus;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId;

public class CrunchyrollSeriesIdClientTests
{
    private readonly CrunchyrollSeriesIdClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    
    public CrunchyrollSeriesIdClientTests()
    {
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<CrunchyrollSeriesIdClient>>();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();

        _sut = new CrunchyrollSeriesIdClient(
            _mockHttpMessageHandler.ToHttpClient(),
            new PluginConfiguration(),
            logger,
            _crunchyrollSessionRepository);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReturnsSearchResponse_WhenSomethingWasFound_GivenValidName(bool isNameInLowerCase)
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token");
        
        var searchDataItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ =>
            {
                var randomTitle = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                return new CrunchyrollSearchDataItem
                {
                    Id = CrunchyrollIdFaker.Generate(),
                    Title = randomTitle,
                    SlugTitle = CrunchyrollSlugFaker.Generate(randomTitle)
                };
            })
            .ToList();
        
        var crunchyrollSeriesId = CrunchyrollIdFaker.Generate();
        var crunchyrollSeriesSlugTitle = CrunchyrollSlugFaker.Generate();
        searchDataItems.Add(new CrunchyrollSearchDataItem
        {
            Id = crunchyrollSeriesId,
            Title = isNameInLowerCase ? name.ToLower() : name,
            SlugTitle = crunchyrollSeriesSlugTitle
        });

        var mockedResponse = new CrunchyrollSearchResponse
        {
            Data =
            [
                new CrunchyrollSearchData
                {
                    Items = searchDataItems
                }
            ]
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US")
            .Respond("application/json", JsonSerializer.Serialize(mockedResponse));
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeTrue();

        searchResponseResult.Value!.Should().Be(crunchyrollSeriesId);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsSearchResponse_WhenCrunchyrollTitleHasSpecialCharSuffix_GivenValidName()
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token");
        
        var searchDataItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ =>
            {
                var randomTitle = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                return new CrunchyrollSearchDataItem
                {
                    Id = CrunchyrollIdFaker.Generate(),
                    Title = randomTitle,
                    SlugTitle = CrunchyrollSlugFaker.Generate(randomTitle)
                };
            })
            .ToList();
        
        var crunchyrollSeriesId = CrunchyrollIdFaker.Generate();
        var crunchyrollSeriesSlugTitle = CrunchyrollSlugFaker.Generate();
        searchDataItems.Add(new CrunchyrollSearchDataItem
        {
            Id = crunchyrollSeriesId,
            Title = $"{name}{new Faker().Random.Char()}",
            SlugTitle = crunchyrollSeriesSlugTitle
        });

        var mockedResponse = new CrunchyrollSearchResponse
        {
            Data =
            [
                new CrunchyrollSearchData
                {
                    Items = searchDataItems
                }
            ]
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US")
            .Respond("application/json", JsonSerializer.Serialize(mockedResponse));
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeTrue();

        searchResponseResult.Value!.Should().Be(crunchyrollSeriesId);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsNull_WhenCrunchyrollTitleHasSuffix_GivenValidName()
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token");
        
        var searchDataItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ =>
            {
                var randomTitle = $"{new Faker().Random.Words()}-{Random.Shared.Next(9999)}";
                return new CrunchyrollSearchDataItem
                {
                    Id = CrunchyrollIdFaker.Generate(),
                    Title = randomTitle,
                    SlugTitle = CrunchyrollSlugFaker.Generate(randomTitle)
                };
            })
            .ToList();
        
        var crunchyrollSeriesId = CrunchyrollIdFaker.Generate();
        var crunchyrollSeriesSlugTitle = CrunchyrollSlugFaker.Generate();
        searchDataItems.Add(new CrunchyrollSearchDataItem
        {
            Id = crunchyrollSeriesId,
            Title = $"{name} {new Faker().Random.Word()}",
            SlugTitle = crunchyrollSeriesSlugTitle
        });

        var mockedResponse = new CrunchyrollSearchResponse
        {
            Data =
            [
                new CrunchyrollSearchData
                {
                    Items = searchDataItems
                }
            ]
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US")
            .Respond("application/json", JsonSerializer.Serialize(mockedResponse));
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeTrue();

        searchResponseResult.Value!.Should().BeNull();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenCrunchyrollSessionNotSet_GivenValidName()
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US");
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeFalse();
        searchResponseResult.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSessionMissing);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(0);
    }

    [Fact]
    public async Task ReturnsFailed_WhenRequestIsNotSuccessful_GivenValidName()
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token123");

        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US")
            .Respond(HttpStatusCode.Forbidden);
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeFalse();
        searchResponseResult.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSearchFailed);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenJsonIsNull_GivenValidName()
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token123");

        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US")
            .Respond("application/json", "null");
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeFalse();
        searchResponseResult.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSearchContentIncompatible);
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
    
    [Fact]
    public async Task ReturnsNull_WhenNothingWasFound_GivenValidName()
    {
        //Arrange
        var name = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token");

        var mockedResponse = new CrunchyrollSearchResponse
        {
            Data = []
        };
        
        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(name)}&n=6&type=series,movie_listing&ratings=true&locale=en-US")
            .Respond("application/json", JsonSerializer.Serialize(mockedResponse));
        
        //Act
        var searchResponseResult = await _sut.GetSeriesIdAsync(name, new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        searchResponseResult.IsSuccess.Should().BeTrue();
        searchResponseResult.Value.Should().BeNull();
        
        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }
}