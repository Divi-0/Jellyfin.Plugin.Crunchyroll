using System.Globalization;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.GetReviews;

public class GetReviewsQueryTests
{
    private readonly Fixture _fixture;

    private readonly GetReviewsQueryHandler _sut;
    private readonly ICrunchyrollGetReviewsClient _crunchyrollGetReviewsClient;
    private readonly ILibraryManager _libraryManager;
    private readonly PluginConfiguration _config;
    private readonly IGetReviewsRepository _getReviewsRepository;
    private readonly ILoginService _loginService;

    public GetReviewsQueryTests()
    {
        _fixture = new Fixture();

        _config = new PluginConfiguration();
        _crunchyrollGetReviewsClient = Substitute.For<ICrunchyrollGetReviewsClient>();
        _getReviewsRepository = Substitute.For<IGetReviewsRepository>();
        _libraryManager = Tests.MockHelper.LibraryManager;
        _loginService = Substitute.For<ILoginService>();
        _sut = new GetReviewsQueryHandler(_crunchyrollGetReviewsClient, _libraryManager, _config,
            _getReviewsRepository, _loginService);
    }
    
    [Fact(Skip = "temp disabled, to not show reviews section with real api, can be enabled again if found a way to not show reviews section on none anime items")]
    public async Task ReturnsReviews_WhenFeatureReviewsIsDisabled_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var series = SeriesFaker.GenerateWithTitleId();

        _config.IsFeatureReviewsEnabled = false;

        _libraryManager
            .RetrieveItem(id)
            .Returns(series);
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        var reviewsResponse = _fixture.Create<ReviewsResponse>();
        _crunchyrollGetReviewsClient
            .GetReviewsAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], pageNumber, pageSize, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(reviewsResponse));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        _libraryManager
            .Received(1)
            .RetrieveItem(id);

        await _crunchyrollGetReviewsClient
            .Received(1)
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
                new CultureInfo("en-US"), 
                Arg.Any<CancellationToken>());

        await _getReviewsRepository
            .DidNotReceive()
            .GetReviewsForTitleIdAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

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
    
    [Fact(Skip = "temp disabled, to not show reviews section with real api, can be enabled again if found a way to not show reviews section on none anime items")]
    public async Task ForwardsError_WhenClientRequestFailed_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var series = SeriesFaker.GenerateWithTitleId();
        
        _config.IsFeatureReviewsEnabled = false;

        _libraryManager
            .RetrieveItem(id)
            .Returns(series);

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var errorCode = "999";
        _crunchyrollGetReviewsClient
            .GetReviewsAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], pageNumber, pageSize, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(errorCode));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Message.Equals(errorCode));
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsReviews_WhenWaybackMachineIsEnabled_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var series = SeriesFaker.GenerateWithTitleId();

        _config.IsFeatureReviewsEnabled = true;

        _libraryManager
            .RetrieveItem(id)
            .Returns(series);
        
        var reviewsResponse = _fixture.Create<ReviewsResponse>();
        _getReviewsRepository
            .GetReviewsForTitleIdAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())!
            .Returns(Result.Ok(reviewsResponse.Reviews));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        _libraryManager
            .Received(1)
            .RetrieveItem(id);
        
        await _getReviewsRepository
            .Received(1)
            .GetReviewsForTitleIdAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _crunchyrollGetReviewsClient
            .DidNotReceive()
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
               Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());

        result.Value!.Reviews.Should().AllSatisfy(actual =>
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
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsNull_WhenFeatureReviewsIsEnabledAndRepositoryReturnsNull_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;
        var series = SeriesFaker.GenerateWithTitleId();

        _config.IsFeatureReviewsEnabled = true;

        _libraryManager
            .RetrieveItem(id)
            .Returns(series);
        
        var reviewsResponse = _fixture.Create<ReviewsResponse>();
        _getReviewsRepository
            .GetReviewsForTitleIdAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())!
            .Returns(Result.Ok<IReadOnlyList<ReviewItem>?>(null));
        
        //Act
        var query = new GetReviewsQuery(id.ToString(), pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        _libraryManager
            .Received(1)
            .RetrieveItem(id);
        
        await _getReviewsRepository
            .Received(1)
            .GetReviewsForTitleIdAsync(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _crunchyrollGetReviewsClient
            .DidNotReceive()
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
               Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
        
        await _loginService
            .DidNotReceive()
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact(Skip = "temp disabled, to not show reviews section with real api, can be enabled again if found a way to not show reviews section on none anime items")]
    public async Task ReturnsFailed_WhenLoginFailed_GivenItemId()
    {
        //Arrange
        var id = Guid.NewGuid();
        const int pageNumber = 1;
        const int pageSize = 10;

        _config.IsFeatureReviewsEnabled = false;

        _libraryManager
            .RetrieveItem(id)
            .Returns(SeriesFaker.GenerateWithTitleId());
        
        var errorCode = "999";
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
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
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _crunchyrollGetReviewsClient
            .DidNotReceive()
            .GetReviewsAsync(
                Arg.Any<string>(), 
                Arg.Any<int>(), 
                Arg.Any<int>(), 
                Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
}