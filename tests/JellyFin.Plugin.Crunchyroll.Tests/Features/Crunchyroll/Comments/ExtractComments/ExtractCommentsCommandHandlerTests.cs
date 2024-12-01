using System.Globalization;
using System.Text.Json;
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
    private readonly IExtractCommentsRepository _repository;
    private readonly IWaybackMachineClient _waybackMachineClient;
    private readonly IAddAvatarService _addAvatarService;
    
    private readonly Fixture _fixture;
    private readonly Faker _faker;
    
    public ExtractCommentsCommandHandlerTests()
    {
        _fixture = new Fixture();
        _faker = new Faker();
        
        _htmlCommentsExtractor = Substitute.For<IHtmlCommentsExtractor>();
        _repository = Substitute.For<IExtractCommentsRepository>();
        var config = new PluginConfiguration();
        _waybackMachineClient = Substitute.For<IWaybackMachineClient>();
        _addAvatarService = Substitute.For<IAddAvatarService>();
        var logger = Substitute.For<ILogger<ExtractCommentsCommandHandler>>();

        _sut = new ExtractCommentsCommandHandler(_htmlCommentsExtractor, _repository, config, 
            _waybackMachineClient, _addAvatarService, logger);
    }

    [Fact]
    public async Task StoresExtractedComments_WhenCommandSent_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => CommentItemFaker.Generate()).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(comments);

        EpisodeComments actualEpisodeComments = null!;
        _repository
            .AddCommentsAsync(Arg.Do<EpisodeComments>(x => actualEpisodeComments = x), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var newAvatarUris = new List<string>();
        var oldAvatarUris = new List<string>();
        foreach (var comment in comments)
        {
            var archivedUri = _faker.Internet.UrlWithPath(fileExt: "png");
            newAvatarUris.Add(archivedUri);
            comment.AvatarIconUri = $"{comment.AvatarIconUri}/{archivedUri}";
            oldAvatarUris.Add((string)comment.AvatarIconUri.Clone());
            _addAvatarService
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(archivedUri));
        }
        
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
                new CultureInfo("en-US"),
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>());
        
        actualEpisodeComments.CrunchyrollEpisodeId.Should().Be(episodeId);
        var actualComments = JsonSerializer.Deserialize<CommentItem[]>(actualEpisodeComments.Comments);
        actualComments.Should().BeEquivalentTo(comments, opt => opt
            .Excluding(x => x.AvatarIconUri));
        
        actualComments.Should().AllSatisfy(x =>
        {
            x.AvatarIconUri.Should().BeOneOf(newAvatarUris);
        });
        
        foreach (var oldAvatarUri in oldAvatarUris)
        {
            await _addAvatarService
                .Received()
                .AddAvatarIfNotExists(Arg.Is<string>(x => x == oldAvatarUri), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task StoresCommentsEntityWithZeroComments_WhenSearchResultIsEmpty_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<SearchResponse>>([]));

        EpisodeComments actualEpisodeComments = null!;
        _repository
            .AddCommentsAsync(Arg.Do<EpisodeComments>(x => actualEpisodeComments = x), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
                new CultureInfo("en-US"),
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>());
        
        actualEpisodeComments.CrunchyrollEpisodeId.Should().Be(episodeId);
        actualEpisodeComments.Comments.Should().Be(JsonSerializer.Serialize(Array.Empty<CommentItem>()));
    }
    
    [Fact]
    public async Task CrunchyrollUrlIsWithoutLanguagePath_WhenTwoLetterIsoLanguageNameEn_GivenTitleIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<SearchResponse>>("error"));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
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
            .GetCommentsAsync(Arg.Any<string>(),  Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(comments);
        
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => CommentItemFaker.Generate()).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(comments);
        
        var newAvatarUris = new List<string>();
        foreach (var comment in comments)
        {
            var archivedUri = _faker.Internet.UrlWithPath(fileExt: "png");
            newAvatarUris.Add(archivedUri);
            comment.AvatarIconUri = $"{comment.AvatarIconUri}/{archivedUri}";
            _addAvatarService
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>())
                .Returns(Result.Fail("error"));
        }

        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        comments.Should().AllSatisfy(x =>
        {
            var isOneOf = newAvatarUris.Contains(x.AvatarIconUri!);
            isOneOf.Should().BeFalse();
        });

        foreach (var comment in comments)
        {
            await _addAvatarService
                .Received(1)
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task ReturnsFailed_WhenWaybackMachineFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
                new CultureInfo("en-US"),
                Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoresEmptyCommentsList_WhenExtractorFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("error"));
        
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
                new CultureInfo("en-US"),
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Is<EpisodeComments>(x => x.Comments == JsonSerializer.Serialize(Array.Empty<CommentItem>(), new JsonSerializerOptions())), 
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsSuccessAndIgnoresExtraction_WhenCommentsAlreadyExist_GivenEpisodeIdAndSlugTitleAndExistingComments()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();

        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(true);

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _repository
            .Received(1)
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>());

        await _waybackMachineClient
            .DidNotReceive()
            .SearchAsync(Arg.Any<string>(),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .DidNotReceive()
            .GetCommentsAsync(Arg.Any<string>(), 
                Arg.Any<CultureInfo>(),
                Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task RetriesNextTimestamp_WhenHtmlRequestFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<CommentItem>>(ExtractCommentsErrorCodes.HtmlUrlRequestFailed));
        
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
                    new CultureInfo("en-US"), Arg.Any<CancellationToken>());
        }
        
        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Is<EpisodeComments>(x => x.Comments == JsonSerializer.Serialize(Array.Empty<CommentItem>(), new JsonSerializerOptions())), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task RetriesNextTimestamp_WhenHtmlIsInvalid_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<IReadOnlyList<CommentItem>>(ExtractCommentsErrorCodes.HtmlExtractorInvalidCrunchyrollCommentsPage));
        
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
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
                    new CultureInfo("en-US"), Arg.Any<CancellationToken>());
        }
        
        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Is<EpisodeComments>(x => x.Comments == JsonSerializer.Serialize(Array.Empty<CommentItem>(), new JsonSerializerOptions())), 
                Arg.Any<CancellationToken>());
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenCommentsExistsFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();

        var error = Guid.NewGuid().ToString();
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryAddCommentsFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => CommentItemFaker.Generate()).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(comments);

        var error = Guid.NewGuid().ToString();
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        foreach (var comment in comments)
        {
            var archivedUri = _faker.Internet.UrlWithPath(fileExt: "png");
            comment.AvatarIconUri = $"{comment.AvatarIconUri}/{archivedUri}";
            _addAvatarService
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(archivedUri));
        }

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/watch/{episodeId}/{episodeSlugTitle}"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .Received(1)
            .GetCommentsAsync(Arg.Is<string>(x => x.Contains($"/watch/{episodeId}/{episodeSlugTitle}")),
                new CultureInfo("en-US"),
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositorySaveChangesFails_GivenEpisodeIdAndSlugTitle()
    {
        //Arrange
        var episodeId = CrunchyrollIdFaker.Generate();
        var episodeSlugTitle = CrunchyrollSlugFaker.Generate();
        
        _repository
            .CommentsForEpisodeExistsAsync(episodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        var searchResponses = _fixture.CreateMany<SearchResponse>().ToList();
        _waybackMachineClient
            .SearchAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(searchResponses);
        
        var comments = Enumerable.Range(0, 10).Select(_ => CommentItemFaker.Generate()).ToList();
        _htmlCommentsExtractor
            .GetCommentsAsync(Arg.Any<string>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(comments);
        
        _repository
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var error = Guid.NewGuid().ToString();
        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        foreach (var comment in comments)
        {
            var archivedUri = _faker.Internet.UrlWithPath(fileExt: "png");
            comment.AvatarIconUri = $"{comment.AvatarIconUri}/{archivedUri}";
            _addAvatarService
                .AddAvatarIfNotExists(comment.AvatarIconUri!, Arg.Any<CancellationToken>())
                .Returns(Result.Ok(archivedUri));
        }

        //Act
        var command = new ExtractCommentsCommand(episodeId, episodeSlugTitle, new CultureInfo("en-US"));
        var result = await _sut.Handle(command, CancellationToken.None);

        //Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be(error);

        await _waybackMachineClient
            .Received(1)
            .SearchAsync(Arg.Is<string>(x => x.Contains(HttpUtility.UrlEncode($"/watch/{episodeId}/{episodeSlugTitle}"))),
                new DateTime(2024, 07, 10),
                Arg.Any<CancellationToken>());
        
        await _htmlCommentsExtractor
            .Received(1)
            .GetCommentsAsync(Arg.Is<string>(x => x.Contains($"/watch/{episodeId}/{episodeSlugTitle}")),
                new CultureInfo("en-US"),
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddCommentsAsync(Arg.Any<EpisodeComments>(), Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}