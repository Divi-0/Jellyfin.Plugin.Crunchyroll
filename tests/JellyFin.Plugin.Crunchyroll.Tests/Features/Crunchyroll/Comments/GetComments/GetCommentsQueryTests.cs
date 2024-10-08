﻿using AutoFixture;
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

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Comments.GetComments;

public class GetCommentsQueryTests
{
    private readonly ICrunchyrollGetCommentsClient _crunchyrollClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILoginService _loginService;
    private readonly PluginConfiguration _config;
    private readonly IGetCommentsSession _session;

    private readonly GetCommentsQueryHandler _sut;
    
    private readonly Fixture _fixture;

    public GetCommentsQueryTests()
    {
        _fixture = new Fixture();
        
        _crunchyrollClient = Substitute.For<ICrunchyrollGetCommentsClient>();
        _libraryManager = MockHelper.LibraryManager;
        _loginService = Substitute.For<ILoginService>();
        _config = new PluginConfiguration();
        _session = Substitute.For<IGetCommentsSession>();

        _sut = new GetCommentsQueryHandler(
            _crunchyrollClient, 
            _libraryManager,
            _loginService,
            _config,
            _session
        );
    }

    [Fact]
    public async Task ReturnsPaginatedComments_WhenTitleIdIsUsedToGetComments_GivenJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsWaybackMachineEnabled = false;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);
            
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _crunchyrollClient
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageNumber, pageSize, Arg.Any<CancellationToken>())
            .Returns(new CommentsResponse());

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
            
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());
        
        await _session
            .DidNotReceive()
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber);
    }

    [Fact]
    public async Task ReturnsFailed_WhenLoginFails_GivenJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsWaybackMachineEnabled = false;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);
            
        var error = Guid.NewGuid().ToString();
        _loginService
            .LoginAnonymously(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message.Equals(error));
            
        await _loginService
            .Received(1)
            .LoginAnonymously(Arg.Any<CancellationToken>());

        await _crunchyrollClient
            .DidNotReceive()
            .GetCommentsAsync(jellyfinId, pageNumber, pageSize, Arg.Any<CancellationToken>());
        
        await _session
            .DidNotReceive()
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber);
    }

    [Fact]
    public async Task ReturnsComments_WhenWaybackMachineIsEnabled_GivenEpisodeJellyfinId()
    {
        //Arrange
        var jellyfinId = Guid.NewGuid().ToString();
        const int pageNumber = 1;
        const int pageSize = 10;
            
        _config.IsWaybackMachineEnabled = true;

        var episode = EpisodeFaker.GenerateWithEpisodeId();
        _libraryManager
            .RetrieveItem(Guid.Parse(jellyfinId))
            .Returns(episode);

        var comments = _fixture.Create<List<CommentItem>>();
        _session
            .GetCommentsAsync(Arg.Any<string>(), pageSize, pageNumber)
            .Returns(comments);

        //Act
        var query = new GetCommentsQuery(jellyfinId, pageNumber, pageSize);
        var result = await _sut.Handle(query, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var commentsResponse = result.Value;
        commentsResponse.Comments.Should().BeEquivalentTo(comments, o => 
            o.Excluding(x => x.AvatarIconUri));

        commentsResponse.Comments.Should().AllSatisfy(comment =>
        {
            comment.AvatarIconUri.Should().Contain("/api/crunchyrollPlugin/crunchyroll/avatar");
        }, because: "avatar icons are cached in the database");

        _libraryManager
            .Received(1)
            .RetrieveItem(Guid.Parse(jellyfinId));

        await _session
            .Received(1)
            .GetCommentsAsync(episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId], pageSize, pageNumber);

        await _crunchyrollClient
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), 
                Arg.Any<CancellationToken>());

    }
}