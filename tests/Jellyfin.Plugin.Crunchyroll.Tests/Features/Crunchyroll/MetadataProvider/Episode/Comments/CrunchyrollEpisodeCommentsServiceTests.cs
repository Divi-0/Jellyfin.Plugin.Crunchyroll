using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments.ExtractComments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Library;
using Mediator;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Episode.Comments;

public class CrunchyrollEpisodeCommentsServiceTests
{
    private readonly CrunchyrollEpisodeCommentsService _sut;
    private readonly IMediator _mediator;
    private readonly PluginConfiguration _config;

    public CrunchyrollEpisodeCommentsServiceTests()
    {
        _mediator = Substitute.For<IMediator>();
        _config = new PluginConfiguration
        {
            IsFeatureCommentsEnabled = true
        };
        _sut = new CrunchyrollEpisodeCommentsService(_mediator, _config);
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenSuccessful_GivenEpisodeWithIds()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        var language = episode.GetPreferredMetadataCultureInfo();

        _mediator
            .Send(Arg.Any<ExtractCommentsCommand>())
            .Returns(Result.Ok());
        
        //Act
        var itemUpdateType = await _sut.ScrapCommentsAsync(episode, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .Received(1)
            .Send(Arg.Is<ExtractCommentsCommand>(x =>
                x.EpisodeId == episodeId &&
                x.Language.Name == language.Name),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenMediatorFailed_GivenEpisodeWithIds()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();
        var episodeId = episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId];
        var language = episode.GetPreferredMetadataCultureInfo();

        _mediator
            .Send(Arg.Any<ExtractCommentsCommand>())
            .Returns(Result.Fail(Guid.NewGuid().ToString()));
        
        //Act
        var itemUpdateType = await _sut.ScrapCommentsAsync(episode, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .Received(1)
            .Send(Arg.Is<ExtractCommentsCommand>(x =>
                x.EpisodeId == episodeId &&
                x.Language.Name == language.Name),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenHasNoEpisodeId_GivenEpisodeWithoutIds()
    {
        //Arrange
        var episode = EpisodeFaker.Generate();
        
        //Act
        var itemUpdateType = await _sut.ScrapCommentsAsync(episode, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsItemUpdateTypeNone_WhenFeatureReviewsIsDisabled_GivenEpisodeWithoutIds()
    {
        //Arrange
        var episode = EpisodeFaker.GenerateWithEpisodeId();

        _config.IsFeatureCommentsEnabled = false;
        
        //Act
        var itemUpdateType = await _sut.ScrapCommentsAsync(episode, CancellationToken.None);

        //Assert
        itemUpdateType.Should().Be(ItemUpdateType.None);

        await _mediator
            .DidNotReceive()
            .Send(Arg.Any<ExtractCommentsCommand>(), Arg.Any<CancellationToken>());
    }
}