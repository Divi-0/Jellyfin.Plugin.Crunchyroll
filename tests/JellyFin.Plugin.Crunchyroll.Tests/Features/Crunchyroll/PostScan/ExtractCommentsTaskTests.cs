using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using Mediator;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class ExtractCommentsTaskTests
{
    private readonly ExtractCommentsTask _sut;

    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;
    
    public ExtractCommentsTaskTests()
    {
        _mediator = Substitute.For<IMediator>();
        _config = new PluginConfiguration();
        var logger = Substitute.For<ILogger<ExtractCommentsTask>>();

        _sut = new ExtractCommentsTask(_mediator, _config, logger);
    }
    
    [Fact]
    public async Task CallsMediatorCommand_WhenSuccessful_GivenEpisodeWithIdAndSlugTitle()
    {
        //Arrange
        var series = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = series.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        var slugTitle = series.ProviderIds[CrunchyrollExternalKeys.EpisodeSlugTitle];
        
        _mediator
            .Send(new ExtractCommentsCommand(episodeId, slugTitle), 
                Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .Received(1)
            .Send(new ExtractCommentsCommand(episodeId, slugTitle),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotCallMediatorCommand_WhenEpisodeHasNoTitleId_GivenEpisodeWithNoTitleId()
    {
        //Arrange
        var series = EpisodeFaker.Generate();
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotCallMediatorCommand_WhenEpisodeHasNoSlugTitle_GivenEpisodeWithNoSlugTitle()
    {
        //Arrange
        var series = EpisodeFaker.Generate();
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotCallMediatorCommand_WhenWaybackMachineIsDisabled_GivenEpisodeWithTitleId()
    {
        //Arrange
        var series = EpisodeFaker.GenerateWithEpisodeId();
        
        _config.IsWaybackMachineEnabled = false;
        
        //Act
        await _sut.RunAsync(series, CancellationToken.None);

        //Assert
        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }
}