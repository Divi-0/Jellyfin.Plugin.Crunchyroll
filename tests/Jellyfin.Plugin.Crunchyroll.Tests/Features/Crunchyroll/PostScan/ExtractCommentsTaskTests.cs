using System.Globalization;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class ExtractCommentsTaskTests
{
    private readonly ExtractCommentsTask _sut;

    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;
    
    public ExtractCommentsTaskTests()
    {
        _mediator = Substitute.For<IMediator>();
        _config = new PluginConfiguration
        {
            IsWaybackMachineEnabled = true
        };
        var logger = Substitute.For<ILogger<ExtractCommentsTask>>();

        _sut = new ExtractCommentsTask(_mediator, _config, logger);
    }
    
    public static IEnumerable<object[]> RandomEpisodeAndMovieWithIds()
    {
        yield return [EpisodeFaker.GenerateWithEpisodeId()];
        yield return [MovieFaker.GenerateWithCrunchyrollIds()];
    }

    public static IEnumerable<object[]> RandomEpisodeAndMovieWithoutIds()
    {
        yield return [EpisodeFaker.Generate()];
        yield return [MovieFaker.Generate()];
    }
    
    [Theory]
    [MemberData(nameof(RandomEpisodeAndMovieWithIds))]
    public async Task CallsMediatorCommand_WhenSuccessful_GivenEpisodeWithIdAndSlugTitle(BaseItem baseItem)
    {
        //Arrange
        var episodeId = baseItem.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        var slugTitle = baseItem.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle];
        
        _mediator
            .Send(new ExtractCommentsCommand(episodeId, slugTitle, new CultureInfo("en-US")), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .Received(1)
            .Send(new ExtractCommentsCommand(episodeId, slugTitle, new CultureInfo("en-US")),
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomEpisodeAndMovieWithoutIds))]
    public async Task DoesNotCallMediatorCommand_WhenEpisodeHasNoTitleId_GivenEpisodeWithNoTitleId(BaseItem baseItem)
    {
        //Arrange
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomEpisodeAndMovieWithoutIds))]
    public async Task DoesNotCallMediatorCommand_WhenEpisodeHasNoSlugTitle_GivenEpisodeWithNoSlugTitle(BaseItem baseItem)
    {
        //Arrange
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RandomEpisodeAndMovieWithIds))]
    public async Task DoesNotCallMediatorCommand_WhenWaybackMachineIsDisabled_GivenEpisodeWithTitleId(BaseItem baseItem)
    {
        //Arrange
        _config.IsWaybackMachineEnabled = false;
        
        //Act
        await _sut.RunAsync(baseItem, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }
}