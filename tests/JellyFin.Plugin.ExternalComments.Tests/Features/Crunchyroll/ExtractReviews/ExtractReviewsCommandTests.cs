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
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.ExtractReviews;

public class ExtractReviewsCommandTests
{
    private readonly Fixture _fixture;
    
    private readonly ExtractReviewsCommandHandler _sut;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly PluginConfiguration _config;
    private readonly IHtmlReviewsExtractor _htmlReviewsExtractor;
    private readonly IAddReviewsSession _addReviewsSession;
    private readonly IGetReviewsSession _getReviewsSession;
    private readonly ILogger<ExtractReviewsCommandHandler> _logger;
    
    public ExtractReviewsCommandTests()
    {
        _fixture = new Fixture();
        
        _waybackMachineClient = Substitute.For<IWaybackMachineClient>();
        _config = new PluginConfiguration();
        _htmlReviewsExtractor = Substitute.For<IHtmlReviewsExtractor>();
        _addReviewsSession = Substitute.For<IAddReviewsSession>();
        _getReviewsSession = Substitute.For<IGetReviewsSession>();
        _logger = Substitute.For<ILogger<ExtractReviewsCommandHandler>>();
        _sut = new ExtractReviewsCommandHandler(_waybackMachineClient, _config, _htmlReviewsExtractor, 
            _addReviewsSession, _getReviewsSession, _logger);
    }

    [Fact]
    public async Task ReturnsSuccess_WhenFoundSnapshot_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));
        
        var availabilityResponse = new AvailabilityResponse()
        {
            ArchivedSnapshots = new Snapshot()
            {
                Closest = new ClosestSnapshot()
                {
                    Timestamp = new DateTime(2024, 07, 1).ToString("yyyyMMddHHmmss"),
                    Available = true,
                    Url = _fixture.Create<Uri>().AbsoluteUri,
                    Status = "200"
                }
            }
        };
        
        _waybackMachineClient.GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(availabilityResponse)));

        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        _htmlReviewsExtractor
            .GetReviewsAsync(availabilityResponse.ArchivedSnapshots.Closest.Url, Arg.Any<CancellationToken>())
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
            .GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
                Arg.Any<CancellationToken>());
        
        await _htmlReviewsExtractor
            .Received(1)
            .GetReviewsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _addReviewsSession
            .Received(1)
            .AddReviewsForTitleIdAsync(titleId, Arg.Any<IReadOnlyList<ReviewItem>>());
    }
    
    [Fact]
    public async Task ReturnsSuccess_WhenFirstRequestWasOverDeletionDateOfReviews_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));

        var availabilityResponseTooNew = new AvailabilityResponse()
        {
            ArchivedSnapshots = new Snapshot()
            {
                Closest = new ClosestSnapshot()
                {
                    Timestamp = new DateTime(2024, 07, 15).ToString("yyyyMMddHHmmss")
                }
            }
        };
        
        _waybackMachineClient.GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(availabilityResponseTooNew)));
        
        var availabilityResponse = new AvailabilityResponse()
        {
            ArchivedSnapshots = new Snapshot()
            {
                Closest = new ClosestSnapshot()
                {
                    Timestamp = new DateTime(2024, 07, 1).ToString("yyyyMMddHHmmss"),
                    Available = true,
                    Url = _fixture.Create<Uri>().AbsoluteUri,
                    Status = "200"
                }
            }
        };
        
        _waybackMachineClient.GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 6 && x.Day == 1),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(availabilityResponse)));

        var reviews = _fixture.CreateMany<ReviewItem>().ToList();
        _htmlReviewsExtractor
            .GetReviewsAsync(availabilityResponse.ArchivedSnapshots.Closest!.Url, Arg.Any<CancellationToken>())
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

        //first request
        await _waybackMachineClient
            .Received(1)
            .GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
                Arg.Any<CancellationToken>());
        
        //second request
        await _waybackMachineClient
            .Received(1)
            .GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 6 && x.Day == 1),
                Arg.Any<CancellationToken>());
        
        await _htmlReviewsExtractor
            .Received(1)
            .GetReviewsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _addReviewsSession
            .Received(1)
            .AddReviewsForTitleIdAsync(titleId, Arg.Any<IReadOnlyList<ReviewItem>>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenGetAvailabilityFails_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var slugTitle = _fixture.Create<string>();
        
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));
        
        var error = "error";
        _waybackMachineClient.GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<AvailabilityResponse>(error)));
        
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
            .GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
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
        
        var availabilityResponse = new AvailabilityResponse()
        {
            ArchivedSnapshots = new Snapshot()
            {
                Closest = new ClosestSnapshot()
                {
                    Timestamp = new DateTime(2024, 07, 1).ToString("yyyyMMddHHmmss"),
                    Available = true,
                    Url = _fixture.Create<Uri>().AbsoluteUri,
                    Status = "200"
                }
            }
        };
        
        _waybackMachineClient.GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Ok(availabilityResponse)));

        var error = "error123";
        _htmlReviewsExtractor
            .GetReviewsAsync(availabilityResponse.ArchivedSnapshots.Closest.Url, Arg.Any<CancellationToken>())
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
            .GetAvailabilityAsync(
                Arg.Any<string>(),
                Arg.Is<DateTime>(x => x.Year == 2024 && x.Month == 7 && x.Day == 1),
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