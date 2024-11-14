using System.Web;
using AutoFixture;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar.AddAvatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.Entites;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Comments.ExtractComments;

public class ExtractCommentsCommandHandlerTests
{
    private readonly ExtractCommentsCommandHandler _sut;

    private readonly IHtmlCommentsExtractor _htmlCommentsExtractor;
    private readonly IExtractCommentsSession _commentsSession;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly IAddAvatarService _addAvatarService;
    private readonly PluginConfiguration _config;
    
    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    public ExtractCommentsCommandHandlerTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
        
        _htmlCommentsExtractor = Substitute.For<IHtmlCommentsExtractor>();
        _commentsSession = Substitute.For<IExtractCommentsSession>();
        _config = new PluginConfiguration();
        _waybackMachineClient = Substitute.For<IWaybackMachineClient>();
        _addAvatarService = Substitute.For<IAddAvatarService>();
        var logger = Substitute.For<ILogger<ExtractCommentsCommandHandler>>();

        _sut = new ExtractCommentsCommandHandler(_htmlCommentsExtractor, _commentsSession, _config, 
            _waybackMachineClient, _addAvatarService, logger);
    }

    [Fact]
    public async Task StoresExtractedComments_WhenCommandSent_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => CommentItemFaker.Generate()).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(comments);

        EpisodeComments actualEpisodeComments = null!;
        _commentsSession
            .InsertComments(Arg.Do<EpisodeComments>(x => actualEpisodeComments = x))
            .Returns(ValueTask.CompletedTask);

        var avatarUris = new Dictionary<CommentItem, string>();
        foreach (var comment in comments)
        {
            var archivedUri = _faker.Internet.UrlWithPath(fileExt: "png");
            avatarUris[comment] = archivedUri;
            comment.AvatarIconUri = $"{comment.AvatarIconUri}/{archivedUri}";
            _addAvatarService
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(archivedUri));
        }

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/watch/{episodeId}/{episodeSlugTitle}"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .Received(1)
            .GetCommentsAsync(Arg.Is<string>(x => x.Contains($"/watch/{episodeId}/{episodeSlugTitle}")), 
                Arg.Any<CancellationToken>());

        await _commentsSession
            .Received(1)
            .InsertComments(Arg.Any<EpisodeComments>());
        
        actualEpisodeComments.EpisodeId.Should().Be(episodeId);
        actualEpisodeComments.Comments.Should().BeEquivalentTo(comments, opt => opt
            .Excluding(x => x.AvatarIconUri));

        actualEpisodeComments.Comments.Should().AllSatisfy(x =>
        {
            x.AvatarIconUri.Should().Be(avatarUris[x]);
        });
        
        foreach (var comment in comments)
        {
            await _addAvatarService
                .Received()
                .AddAvatarIfNotExists(Arg.Is<string>(x => x.Contains(avatarUris[comment])), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task StoresCommentsEntityWithZeroComments_WhenSearchResultIsEmpty_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);
        
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<SearchResponse>>([]));

        EpisodeComments actualEpisodeComments = null!;
        _commentsSession
            .InsertComments(Arg.Do<EpisodeComments>(x => actualEpisodeComments = x))
            .Returns(ValueTask.CompletedTask);

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/watch/{episodeId}/{episodeSlugTitle}"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .DidNotReceive()
            .GetCommentsAsync(Arg.Is<string>(x => x.Contains($"/watch/{episodeId}/{episodeSlugTitle}")), 
                Arg.Any<CancellationToken>());

        await _commentsSession
            .Received(1)
            .InsertComments(Arg.Any<EpisodeComments>());
        
        actualEpisodeComments.EpisodeId.Should().Be(episodeId);
        actualEpisodeComments.Comments.Should().BeEmpty();
    }
    
    [Fact]
    public async Task CrunchyrollUrlIsWithoutLanguagePath_WhenTwoLetterIsoLanguageNameEn_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();

        _config.CrunchyrollLanguage = "en-US";
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);
        
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<SearchResponse>>("error"));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        _ = await _sut.Handle(command, CancellationToken.None);

        //Assert
        //just check if the crunchyroll url has not "en" in path

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => !x.Contains(HttpUtility.UrlEncode("/en/"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task IgnoresAvatarUri_WhenUriIsEmpty_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => new CommentItem
        {
            Author = "Author",
            Message = "Message",
            AvatarIconUri = string.Empty
        }).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(comments);

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _addAvatarService
            .DidNotReceive()
            .AddAvatarIfNotExists(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task IgnoresAvatarUri_WhenAddAvatarImageFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => CommentItemFaker.Generate()).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(comments);
        
        var avatarUris = new Dictionary<CommentItem, string>();
        foreach (var comment in comments)
        {
            var archivedUri = _faker.Internet.UrlWithPath(fileExt: "png");
            avatarUris[comment] = archivedUri;
            comment.AvatarIconUri = $"{comment.AvatarIconUri}/{archivedUri}";
            _addAvatarService
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>())
                .Returns(Result.Fail("error"));
        }

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        comments.Should().AllSatisfy(x =>
        {
            x.AvatarIconUri.Should().NotBe(avatarUris[x]);
        });

        foreach (var comment in comments)
        {
            await _addAvatarService
                .Received(1)
                .AddAvatarIfNotExists(Arg.Is<string>(x => x.Contains(avatarUris[comment])), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task ReturnsFailed_WhenWaybackMachineFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);
        
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Any<string>(),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), 
                Arg.Any<CancellationToken>());

        await _commentsSession
            .DidNotReceive()
            .InsertComments(Arg.Any<EpisodeComments>());
    }

    [Fact]
    public async Task StoresEmptyCommentsList_WhenExtractorFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);
        
        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Any<string>(),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .Received(3)
            .GetCommentsAsync(Arg.Any<string>(), 
                Arg.Any<CancellationToken>());

        await _commentsSession
            .Received(1)
            .InsertComments(Arg.Is<EpisodeComments>(x => x.Comments.Count == 0));
    }

    [Fact]
    public async Task ReturnsSuccessAndIgnoresExtraction_WhenCommentsAlreadyExist_GivenEpisodeIdAndSlugTitleAndExistingComments()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();

        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(true);

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _commentsSession
            .Received(1)
            .CommentsForEpisodeExists(episodeId);

        await _waybackMachineClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), 
                Arg.Any<CancellationToken>());

        await _commentsSession
            .DidNotReceive()
            .InsertComments(Arg.Any<EpisodeComments>());
    }
    
    [Fact]
    public async Task RetriesNextTimestamp_WhenHtmlRequestFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<CommentItem>>(ExtractCommentsErrorCodes.HtmlUrlRequestFailed));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/watch/{episodeId}/{episodeSlugTitle}"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());

        foreach (var searchResponse in searchResponses)
        {
            //Should call all searchResponses, because every request fails
            await _htmlCommentsExtractor
                .Received(1)
                .GetCommentsAsync(Arg.Is<string>(x => 
                        x.Contains($"/watch/{episodeId}/{episodeSlugTitle}") && 
                        x.Contains(searchResponse.Timestamp.ToString("yyyyMMddHHmmss"))), 
                    Arg.Any<CancellationToken>());
        }
        
        await _commentsSession
            .Received(1)
            .InsertComments(Arg.Is<EpisodeComments>(x => x.Comments.Count == 0));
    }
    
    [Fact]
    public async Task RetriesNextTimestamp_WhenHtmlIsInvalid_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _commentsSession
            .CommentsForEpisodeExists(episodeId)
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<CommentItem>>(ExtractCommentsErrorCodes.HtmlExtractorInvalidCrunchyrollCommentsPage));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle);
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/watch/{episodeId}/{episodeSlugTitle}"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());

        foreach (var searchResponse in searchResponses)
        {
            //Should call all searchResponses, because every request fails
            await _htmlCommentsExtractor
                .Received(1)
                .GetCommentsAsync(Arg.Is<string>(x => 
                        x.Contains($"/watch/{episodeId}/{episodeSlugTitle}") && 
                        x.Contains(searchResponse.Timestamp.ToString("yyyyMMddHHmmss"))), 
                    Arg.Any<CancellationToken>());
        }
        
        await _commentsSession
            .Received(1)
            .InsertComments(Arg.Is<EpisodeComments>(x => x.Comments.Count == 0));
    }
}