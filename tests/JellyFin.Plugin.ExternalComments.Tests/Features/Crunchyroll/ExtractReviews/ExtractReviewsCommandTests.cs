using System.Globalization;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.ExternalComments.Tests.Shared.Fixture;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews;

public class ExtractReviewsCommandTests
{
    private readonly IFixture _fixture;
    
    private readonly ExtractReviewsCommandHandler _sut;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly PluginConfiguration _config;
    private readonly IHtmlReviewsExtractor _htmlReviewsExtractor;
    private readonly IAddReviewsSession _addReviewsSession;
    private readonly IGetReviewsSession _getReviewsSession;
    private readonly ILogger<ExtractReviewsCommandHandler> _logger;
    private readonly IAvatarClient _avatarClient;
    
    public ExtractReviewsCommandTests()
    {
        _fixture = new Fixture()
            .Customize(new WaybackMachineSearchResponseCustomization());
        
        _waybackMachineClient = Substitute.For<IWaybackMachineClient>();
        _config = new PluginConfiguration();
        _htmlReviewsExtractor = Substitute.For<IHtmlReviewsExtractor>();
        _addReviewsSession = Substitute.For<IAddReviewsSession>();
        _getReviewsSession = Substitute.For<IGetReviewsSession>();
        _logger = Substitute.For<ILogger<ExtractReviewsCommandHandler>>();
        _avatarClient = Substitute.For<IAvatarClient>();
        _sut = new ExtractReviewsCommandHandler(_waybackMachineClient, _config, _htmlReviewsExtractor, 
            _addReviewsSession, _getReviewsSession, _logger, _avatarClient);
    }

    [Fact]
    public async Task ReturnsSuccess_WhenSnapshotFound_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));

        var searchResponse = _fixture.Create<SearchResponse>();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(searchResponse)));

        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        var url = Path.Combine(
            _config.ArchiveOrgUrl, 
            "web", 
            searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
            _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
            new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
            "series",
            titleId,
            slugTitle)
            .Replace('\\', '/');
        
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(reviews)));
        
        _addReviewsSession
            .AddReviewsForTitleIdAsync(titleId, Arg.Any<IReadOnlyList<ReviewItem>>())
            .Returns(ValueTask.CompletedTask);
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle
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
        
        await _htmlReviewsExtractor
            .Received(1)
            .GetReviewsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _addReviewsSession
            .Received(1)
            .AddReviewsForTitleIdAsync(titleId, Arg.Any<IReadOnlyList<ReviewItem>>());
    }

    [Fact]
    public async Task StoresAvatarImages_WhenSnapshotFound_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));

        var searchResponse = _fixture.Create<SearchResponse>();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(searchResponse)));

        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        var url = Path.Combine(
            _config.ArchiveOrgUrl, 
            "web", 
            searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
            _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
            new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
            "series",
            titleId,
            slugTitle)
            .Replace('\\', '/');
        
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(reviews)));
        
        _addReviewsSession
            .AddReviewsForTitleIdAsync(titleId, Arg.Any<IReadOnlyList<ReviewItem>>())
            .Returns(ValueTask.CompletedTask);

        var streams = new List<Stream>();
        foreach (var review in reviews)
        {
            var stream = new MemoryStream(_fixture.Create<byte[]>());
            streams.Add(stream);
            _avatarClient
                .GetAvatarStreamAsync(review.Author.AvatarUri, Arg.Any<CancellationToken>())
                .Returns(Result.Ok<Stream>(stream));
        }
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        reviews.Should().AllSatisfy(x =>
        {
            _avatarClient
                .Received(1)
                .GetAvatarStreamAsync(x.Author.AvatarUri, Arg.Any<CancellationToken>());
            
            _addReviewsSession
                .Received(1)
                .AddAvatarImageAsync(x.Author.AvatarUri, Arg.Is<Stream>(actualStream => streams.Any(expectedStream => expectedStream == actualStream)));
        });
    }

    [Fact]
    public async Task ReturnsFailed_WhenSearchFails_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));
        
        var error = "error";
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<SearchResponse>(error)));
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle
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
    public async Task ReturnsFailed_WhenExtractReviewsFails_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));
        
        var searchResponse = _fixture.Create<SearchResponse>();
        
        _waybackMachineClient.SearchAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 10),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(searchResponse)));

        var url = Path.Combine(
                _config.ArchiveOrgUrl, 
                "web", 
                searchResponse.Timestamp.ToString("yyyyMMddHHmmss"),
                _config.CrunchyrollUrl.Contains("www") ? _config.CrunchyrollUrl.Split("www.")[1] : _config.CrunchyrollUrl.Split("//")[1],
                new CultureInfo(_config.CrunchyrollLanguage).TwoLetterISOLanguageName,
                "series",
                titleId,
                slugTitle)
            .Replace('\\', '/');
        
        var error = "error123";
        _htmlReviewsExtractor
            .GetReviewsAsync(url, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<IReadOnlyList<ReviewItem>>(error)));
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle
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
    public async Task ReturnsAlreadyHasReviews_WhenTitleHasAlreadyReviews_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok(_fixture.Create<IReadOnlyList<ReviewItem>?>()));
        
        //Act
        var command = new ExtractReviewsCommand()
        {
            TitleId = titleId,
            SlugTitle = slugTitle
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == ExtractReviewsErrorCodes.TitleAlreadyHasReviews);

        await _getReviewsSession
            .Received(1)
            .GetReviewsForTitleIdAsync(titleId);
    }
}