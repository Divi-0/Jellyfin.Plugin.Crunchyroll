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
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Search;

public class CrunchyrollSearchClientTests
{
    private readonly CrunchyrollSearchClient _sut;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public CrunchyrollSearchClientTests()
    {
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        var logger = Substitute.For<ILogger<CrunchyrollSearchClient>>();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();

        _sut = new CrunchyrollSearchClient(
            _mockHttpMessageHandler.ToHttpClient(),
            new PluginConfiguration(),
            logger,
            _crunchyrollSessionRepository);
    }

    [Fact]
    public async Task ReturnsAllItems_WhenRequestWasSuccessful_GivenQuery()
    {
        //Arrange
        var query = new Faker().Random.Words();

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token");

        var items = Enumerable.Range(0, Random.Shared.Next(1, 10))
            .Select(_ => new CrunchyrollSearchDataItem
            {
                Id = CrunchyrollIdFaker.Generate(),
                Title = new Faker().Random.Words(),
                SlugTitle = CrunchyrollSlugFaker.Generate()
            })
            .ToList();

        var mockedResponse = new CrunchyrollSearchResponse
        {
            Data =
            [
                new CrunchyrollSearchData { Items = items.Take(1).ToList() },
                new CrunchyrollSearchData { Items = items.Skip(1).ToList() }
            ]
        };

        var mockedRequest = _mockHttpMessageHandler
            .When($"https://www.crunchyroll.com/content/v2/discover/search?q={UrlEncoder.Default.Encode(query)}&n=10&type=series&ratings=true&locale=en-US")
            .WithHeaders("Authorization", "Bearer token")
            .Respond("application/json", JsonSerializer.Serialize(mockedResponse));

        //Act
        var result = await _sut.SearchAsync(query, "series", new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(x => x.Id).Should().BeEquivalentTo(items.Select(x => x.Id));

        _mockHttpMessageHandler.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task ReturnsFailed_WhenRequestFailed_GivenQuery()
    {
        //Arrange
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns("token");

        _mockHttpMessageHandler
            .When("https://www.crunchyroll.com/content/v2/discover/search*")
            .Respond(HttpStatusCode.BadRequest);

        //Act
        var result = await _sut.SearchAsync("query", "series", new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSearchFailed);
    }

    [Fact]
    public async Task ReturnsFailed_WhenSessionIsMissing_GivenQuery()
    {
        //Arrange
        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);

        //Act
        var result = await _sut.SearchAsync("query", "series", new CultureInfo("en-US"), CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(ErrorCodes.CrunchyrollSessionMissing);
    }
}