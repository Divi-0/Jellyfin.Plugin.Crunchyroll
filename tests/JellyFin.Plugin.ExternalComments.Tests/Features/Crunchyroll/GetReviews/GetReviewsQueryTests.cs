using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Contracts.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.ExternalComments.Tests.Shared;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using NSubstitute;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.GetReviews;

public class GetReviewsQueryTests
{
    private readonly Fixture _fixture;

    private readonly GetReviewsQueryHandler _sut;
    private readonly ICrunchyrollGetReviewsClient _crunchyrollGetReviewsClient;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;
    private readonly IGetReviewsSession _getReviewsSession;
    private readonly ILoginService _loginService;

    public GetReviewsQueryTests()
    {
        _fixture = new Fixture();

        _config = new PluginConfiguration();
        _crunchyrollGetReviewsClient = Substitute.For<ICrunchyrollGetReviewsClient>();
        _getReviewsSession = Substitute.For<IGetReviewsSession>();
        _libraryManager = Tests.MockHelper.LibraryManager;
        _loginService = Substitute.For<ILoginService>();
        _sut = new GetReviewsQueryHandler(_crunchyrollGetReviewsClient, _libraryManager, _config,
            _getReviewsSession, _loginService);
    }
    
    [Fact]
    public async Task ReturnsReviews_WhenWaybackMachineIsDisabled_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var providerId = Guid.NewGuid().ToString();

        _config.IsWaybackMachineEnabled = false;

        _libraryManager.MockRetrieveItem(id, providerId);
        
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var reviewsResponse = _fixture.Create<ReviewsResponse>();
        _crunchyrollGetReviewsClient
            .GetReviewsAsync(providerId, pageNumber, pageSize, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(reviewsResponse));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());

        _libraryManager
            .Received(1)
            .RetrieveItem(id);

        await _crunchyrollGetReviewsClient
            .Received(1)
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
                Arg.Any<CancellationToken>());

        await _getReviewsSession
            .Received(0)
            .GetReviewsForTitleIdAsync(Arg.Any<string>());

        result.Value.Should().Be(reviewsResponse);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenItemWasNotFound_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;

        _libraryManager.MockRetrieveItemNotFound(id);
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Message.Equals(GetReviewsErrorCodes.ItemNotFound));
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenItemHasNoProviderId_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;

        _libraryManager.MockRetrieveItem(id, string.Empty);
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Message.Equals(GetReviewsErrorCodes.ItemHasNoProviderId));
    }
    
    [Fact]
    public async Task ForwardsError_WhenClientRequestFailed_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var providerId = Guid.NewGuid().ToString();
        
        _config.IsWaybackMachineEnabled = false;

        _libraryManager.MockRetrieveItem(id, providerId);

        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var errorCode = "999";
        _crunchyrollGetReviewsClient
            .GetReviewsAsync(providerId, pageNumber, pageSize, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(errorCode));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Message.Equals(errorCode));
        
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsReviews_WhenWaybackMachineIsEnabled_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var titleId = Guid.NewGuid().ToString();

        _config.IsWaybackMachineEnabled = true;

        _libraryManager.MockRetrieveItem(id, titleId);
        
        var reviewsResponse = _fixture.Create<ReviewsResponse>();
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)
            .Returns(ValueTask.FromResult(Result.Ok<IReadOnlyList<ReviewItem>?>(reviewsResponse.Reviews)));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        _libraryManager
            .Received(1)
            .RetrieveItem(id);
        
        await _getReviewsSession
            .Received(1)
            .GetReviewsForTitleIdAsync(titleId);

        await _crunchyrollGetReviewsClient
            .Received(0)
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
                Arg.Any<CancellationToken>());

        result.Value.Reviews.Should().AllSatisfy(actual =>
        {
            reviewsResponse.Reviews.Should().Contain(expected => expected.Title == actual.Title);
            reviewsResponse.Reviews.Should().Contain(expected => expected.Body == actual.Body);
            reviewsResponse.Reviews.Should().Contain(expected => expected.Rating == actual.Rating);
            reviewsResponse.Reviews.Should().Contain(expected => expected.Author.Username == actual.Author.Username);
            reviewsResponse.Reviews.Should().Contain(expected => expected.AuthorRating == actual.AuthorRating);
            reviewsResponse.Reviews.Should().Contain(expected => expected.CreatedAt == actual.CreatedAt);
            actual.Author.AvatarUri.Should().Contain($"/{Routes.Root}/{AvatarConstants.GetAvatarSubRoute}");
        });
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymously(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ForwardsError_WhenReviewsNotFound_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var titleId = Guid.NewGuid().ToString();

        _config.IsWaybackMachineEnabled = true;

        _libraryManager.MockRetrieveItem(id, titleId);
        
        var reviewsResponse = _fixture.Create<ReviewsResponse>();
        _getReviewsSession
            .GetReviewsForTitleIdAsync(titleId)!
            .Returns(ValueTask.FromResult(Result.Ok<IReadOnlyList<ReviewItem>>(null!)));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reviews.Should().BeEmpty();

        _libraryManager
            .Received(1)
            .RetrieveItem(id);
        
        await _getReviewsSession
            .Received(1)
            .GetReviewsForTitleIdAsync(titleId);

        await _crunchyrollGetReviewsClient
            .Received(0)
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenLoginFailed_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var titleId = Guid.NewGuid().ToString();

        _config.IsWaybackMachineEnabled = false;

        _libraryManager.MockRetrieveItem(id, titleId);
        
        var errorCode = "999";
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(errorCode));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Message.Equals(errorCode));

        _libraryManager
            .Received(1)
            .RetrieveItem(id);

        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _crunchyrollGetReviewsClient
            .DidNotReceive()
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
                Arg.Any<CancellationToken>());
    }
}