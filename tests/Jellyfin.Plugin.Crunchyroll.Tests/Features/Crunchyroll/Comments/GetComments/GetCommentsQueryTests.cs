using System.Globalization;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Comments.GetComments;

public class GetCommentsQueryTests
{
    private readonly ICrunchyrollGetCommentsClient _crunchyrollClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILoginService _loginService;
    private readonly PluginConfiguration _config;
    private readonly IGetCommentsRepository _repository;

    private readonly GetCommentsQueryHandler _sut;
    
    private readonly Fixture _fixture;

    public GetCommentsQueryTests()
    {
        _fixture = new Fixture();
        
        _crunchyrollClient = Substitute.For<ICrunchyrollGetCommentsClient>();
        _libraryManager = MockHelper.LibraryManager;
        _loginService = Substitute.For<ILoginService>();
        _config = new PluginConfiguration();
        _repository = Substitute.For<IGetCommentsRepository>();

        _sut = new GetCommentsQueryHandler(
            _crunchyrollClient, 
            _libraryManager,
            _loginService,
            _config,
            _repository
        );
    }

    [Fact(Skip = "temp disabled, to not show comments section with real api, can be enabled again if found a way to not show comment section on none anime items")]
    public async Task ReturnsPaginatedComments_WhenTitleIdIsUsedToGetComments_GivenJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsFeatureCommentsEnabled = false;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);
            
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _crunchyrollClient
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageNumber, pageSize, 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(new CommentsResponse());

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
            
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "temp disabled, to not show comments section with real api, can be enabled again if found a way to not show comment section on none anime items")]
    public async Task ReturnsFailed_WhenLoginFails_GivenJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsFeatureCommentsEnabled = false;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);
            
        var error = Guid.NewGuid().ToString();
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message.Equals(error));
            
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollClient
            .DidNotReceive()
            .GetCommentsAsync(jellyfinId, pageNumber, pageSize,  Arg.Any<CultureInfo>(),Arg.Any<CancellationToken>());
        
        await _repository
            .DidNotReceive()
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsComments_WhenFeatureCommentsIsEnabled_GivenEpisodeJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsFeatureCommentsEnabled = true;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);

        var comments = _fixture.Create<List<CommentItem>>();
        _repository
            .GetCommentsAsync(Arg.Any<string>(), pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(comments);

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var commentsResponse = result.Value;
        commentsResponse!.Comments.Should().BeEquivalentTo(comments, o => 
            o.Excluding(x => x.AvatarIconUri));

        commentsResponse.Comments.Should().AllSatisfy(comment =>
        {
            comment.AvatarIconUri.Should().Contain("/api/crunchyrollPlugin/crunchyroll/avatar");
        }, because: "avatar icons are cached in the database");

        _libraryManager
            .Received(1)
            .RetrieveItem(Guid.Parse(jellyfinId));

        await _repository
            .Received(1)
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _crunchyrollClient
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNull_WhenRepositoryReturnedNull_GivenEpisodeJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsFeatureCommentsEnabled = true;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);

        var comments = _fixture.Create<List<CommentItem>>();
        _repository
            .GetCommentsAsync(Arg.Any<string>(), pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<CommentItem>?>(null));

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();

        _libraryManager
            .Received(1)
            .RetrieveItem(Guid.Parse(jellyfinId));

        await _repository
            .Received(1)
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _crunchyrollClient
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetCommentsFailed_GivenJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
        
        _config.IsFeatureCommentsEnabled = true;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);

        var error = Guid.NewGuid().ToString();
        _repository
            .GetCommentsAsync(Arg.Any<string>(), pageSize, pageNumber, Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        _libraryManager
            .Received(1)
            .RetrieveItem(Guid.Parse(jellyfinId));

        await _repository
            .Received(1)
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber, 
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());

        await _crunchyrollClient
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

    }
}