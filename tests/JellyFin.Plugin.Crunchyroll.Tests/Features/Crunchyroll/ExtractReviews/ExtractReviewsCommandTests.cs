using System.Globalization;
using System.Text.Json;
using System.Web;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.ExtractReviews;

public class ExtractReviewsCommandTests
{
    private readonly IFixture _fixture;
    
    private readonly ExtractReviewsCommandHandler _sut;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly PluginConfiguration _config;
    private readonly IHtmlReviewsExtractor _htmlReviewsExtractor;
    private readonly IAddReviewsRepistory _repistory;
    private readonly IGetReviewsRepository _iGetReviewsRepository;
    private readonly IAddAvatarService _addAvatarService;
    
    public ExtractReviewsCommandTests()
    {
        _fixture = new Fixture()
            .Customize(new WaybackMachineSearchResponseCustomization());
        
        _waybackMachineClient = Substitute.For<IWaybackMachineClient>();
        _config = new PluginConfiguration();
        _htmlReviewsExtractor = Substitute.For<IHtmlReviewsExtractor>();
        _repistory = Substitute.For<IAddReviewsRepistory>();
        _iGetReviewsRepository = Substitute.For<IGetReviewsRepository>();
        var logger = Substitute.For<ILogger<ExtractReviewsCommandHandler>>();
        _addAvatarService = Substitute.For<IAddAvatarService>();
        _sut = new ExtractReviewsCommandHandler(_waybackMachineClient, _config, _htmlReviewsExtractor, 
            _repistory, _iGetReviewsRepository, logger, _addAvatarService);
    }

    [Fact]
    public async Task ReturnsSuccess_WhenSnapshotFound_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        var webArchiveOrgUri = new UriBuilder(Uri.UriSchemeHttp, "web.archive.org").ToString();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<SearchResponse>>(searchResponses)));

        var reviews = _fixture.CreateMany<ReviewItem>().ToList();

        var archivedImageUrls = new Dictionary<ReviewItem, string>();
        foreach (var review in reviews)
        {
            var archivedImageUrl = new Faker().Internet.UrlWithPath(fileExt: "png");
            var uri = $"{webArchiveOrgUri}im_/{archivedImageUrl}";
            
            review.Author.AvatarUri = uri;
            archivedImageUrls[review] = archivedImageUrl;
        }
        
        var url = Path.Combine(
            _config.ArchiveOrgUrl, 
            "web", 
            searchResponses.Last().Timestamp.ToString("yyyyMMddHHmmss"),
            _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
            "series",
            titleId,
            slugTitle)
            .Replace('\\', '/');
        
        _htmlReviewsExtractor
            .GetReviewsAsync(url,  Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(reviews)));

        TitleReviews? actualTitleReviews = null;
        _repistory
            .AddReviewsForTitleIdAsync(Arg.Do<TitleReviews>(x => actualTitleReviews = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repistory
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        foreach (var review in reviews)
        {
            _addAvatarService
                .AddAvatarIfNotExists(review.Author.AvatarUri, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(archivedImageUrls[review]));
        }
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _waybackMachineClient
            .Received(1)
            .SearchAsync(
                Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/{titleId}/{slugTitle}"))),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>());
        
        await _htmlReviewsExtractor
            .Received(1)
            .GetReviewsAsync(Arg.Any<string>(), new CultureInfo("en-US"), Arg.Any<CancellationToken>());
        
        await _repistory
            .Received(1)
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>());

        await _repistory
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());

        actualTitleReviews.Should().NotBeNull();
        var actualReviews = JsonSerializer.Deserialize<ReviewItem[]>(actualTitleReviews!.Reviews);
        actualReviews!.Should().AllSatisfy(x =>
        {
            x.Author.AvatarUri.Should().NotContain(webArchiveOrgUri);
        });
    }

    [Fact]
    public async Task StoresReviewsEntityWithZeroReviews_WhenSearchResultIsEmpty_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<SearchResponse>>([])));

        TitleReviews? actualTitleReviews = null;
        _repistory
            .AddReviewsForTitleIdAsync(Arg.Do<TitleReviews>(x => actualTitleReviews = x),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repistory
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _waybackMachineClient
            .Received(1)
            .SearchAsync(
                Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/{titleId}/{slugTitle}"))),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>());
        
        await _htmlReviewsExtractor
            .DidNotReceive()
            .GetReviewsAsync(Arg.Any<string>(), new CultureInfo("en-US"), Arg.Any<CancellationToken>());
        
        await _repistory
            .Received(1)
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>());

        await _repistory
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());

        actualTitleReviews.Should().NotBeNull();
        actualTitleReviews!.Reviews.Should().Be(JsonSerializer.Serialize(Array.Empty<ReviewItem>()));
    }
    
    [Fact]
    public async Task CrunchyrollUrlIsWithoutLanguagePath_WhenTwoLetterIsoLanguageNameEn_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));

        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<IReadOnlyList<SearchResponse>>("error")));
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        _ = await _sut.Handle(command, CancellationToken.None);

        //Assert
        //just check if the crunchyroll url has not "en" in path
        
        await _waybackMachineClient
            .Received(1)
            .SearchAsync(
                Arg.Is<string>(x => !x.Contains(HttpUtility.UrlEncode("/en/"))),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoresAvatarImages_WhenSnapshotFound_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<SearchResponse>>(searchResponses)));

        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        var url = Path.Combine(
            _config.ArchiveOrgUrl, 
            "web", 
            searchResponses.Last().Timestamp.ToString("yyyyMMddHHmmss"),
            _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
            "series",
            titleId,
            slugTitle)
            .Replace('\\', '/');
        
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(reviews)));
        
        _repistory
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

         _repistory
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        foreach (var review in reviews)
        {
            _addAvatarService
                .AddAvatarIfNotExists(review.Author.AvatarUri, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(review.Author.AvatarUri));
        }
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        reviews.Should().AllSatisfy(x =>
        {
            _addAvatarService
                .Received(1)
                .AddAvatarIfNotExists(x.Author.AvatarUri, Arg.Any<CancellationToken>());
        });
        
        await _repistory
            .Received(1)
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>());

        await _repistory
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenSearchFails_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));
        
        var error = "error";
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<IReadOnlyList<SearchResponse>>(error)));
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == error);

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoresEmptyReviews_WhenExtractReviewsFails_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));
        
        var searchResponse = _fixture.CreateMany<SearchResponse>(1).ToList();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<SearchResponse>>(searchResponse)));

        var url = Path.Combine(
                _config.ArchiveOrgUrl, 
                "web", 
                searchResponse.Last().Timestamp.ToString("yyyyMMddHHmmss"),
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
                "series",
                titleId,
                slugTitle)
            .Replace('\\', '/');
        
        var error = "error123";
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<IReadOnlyList<ReviewItem>>(error)));

        _repistory
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repistory
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>());
        
        await _repistory
            .Received(1)
            .AddReviewsForTitleIdAsync(
                Arg.Is<TitleReviews>(x => JsonSerializer.Deserialize<ReviewItem[]>(x.Reviews, new JsonSerializerOptions())!.Length == 0),
                Arg.Any<CancellationToken>());

        await _repistory
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsAlreadyHasReviews_WhenTitleHasAlreadyReviews_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(_fixture.Create<IReadOnlyList<ReviewItem>>()));
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == ExtractReviewsErrorCodes.TitleAlreadyHasReviews);

        await _iGetReviewsRepository
            .Received(1)
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetriesNextTimestamp_WhenHtmlHadInvalidFormat_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<SearchResponse>>(searchResponses)));

        var htmlUrls = new List<string>();
        
        for (var i = searchResponses.Count - 1; i > 0; i--)
        {
            var failedUrl = Path.Combine(
                    _config.ArchiveOrgUrl, 
                    "web", 
                    searchResponses[i].Timestamp.ToString("yyyyMMddHHmmss"),
                    _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
                    "series",
                    titleId,
                    slugTitle)
                .Replace('\\', '/');
            
            htmlUrls.Add(failedUrl);
        
            _htmlReviewsExtractor
                .GetReviewsAsync(failedUrl, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Fail<IReadOnlyList<ReviewItem>>(ExtractReviewsErrorCodes.HtmlExtractorInvalidCrunchyrollReviewsPage)));
        }
        
        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        var url = Path.Combine(
                _config.ArchiveOrgUrl, 
                "web", 
                searchResponses.First().Timestamp.ToString("yyyyMMddHHmmss"),
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
                "series",
                titleId,
                slugTitle)
            .Replace('\\', '/');
        
        htmlUrls.Add(url);
        
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(reviews)));
        
        _repistory
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repistory
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        foreach (var review in reviews)
        {
            _addAvatarService
                .AddAvatarIfNotExists(review.Author.AvatarUri, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(review.Author.AvatarUri));
        }
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        foreach (var expectedUrl in htmlUrls)
        {
            await _htmlReviewsExtractor
                .Received(1)
                .GetReviewsAsync(expectedUrl, new CultureInfo("en-US"), Arg.Any<CancellationToken>());
        }
        
        await _repistory
            .Received(1)
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>());

        await _repistory
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetriesNextTimestamp_WhenHtmlRequestFailed_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _iGetReviewsRepository
            .GetReviewsForTitleIdAsync(titleId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>>([]));

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<SearchResponse>>(searchResponses)));

        var htmlUrls = new List<string>();
        
        for (var i = searchResponses.Count - 1; i > 0; i--)
        {
            var failedUrl = Path.Combine(
                    _config.ArchiveOrgUrl, 
                    "web", 
                    searchResponses[i].Timestamp.ToString("yyyyMMddHHmmss"),
                    _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
                    "series",
                    titleId,
                    slugTitle)
                .Replace('\\', '/');
            
            htmlUrls.Add(failedUrl);
        
            _htmlReviewsExtractor
                .GetReviewsAsync(failedUrl, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Fail<IReadOnlyList<ReviewItem>>(ExtractReviewsErrorCodes.HtmlUrlRequestFailed)));
        }
        
        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        var url = Path.Combine(
                _config.ArchiveOrgUrl, 
                "web", 
                searchResponses.First().Timestamp.ToString("yyyyMMddHHmmss"),
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
                "series",
                titleId,
                slugTitle)
            .Replace('\\', '/');
        
        htmlUrls.Add(url);
        
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(reviews)));
        
        _repistory
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repistory
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        foreach (var review in reviews)
        {
            _addAvatarService
                .AddAvatarIfNotExists(review.Author.AvatarUri, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(review.Author.AvatarUri));
        }
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle,
            Language = new CultureInfo("en-US")
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        htmlUrls.Should().AllSatisfy(expectedUrl =>
        {
            _htmlReviewsExtractor
                .Received(1)
                .GetReviewsAsync(expectedUrl, new CultureInfo("en-US"), Arg.Any<CancellationToken>());
        });
        
        await _repistory
            .Received(1)
            .AddReviewsForTitleIdAsync(Arg.Any<TitleReviews>(), Arg.Any<CancellationToken>());

        await _repistory
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}