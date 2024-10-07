using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public class EpisodeIdQueryTests
{
    private readonly Fixture _fixture;
    
    private readonly EpisodeIdQueryHandler _sut;
    private readonly IGetEpisodeSession _getEpisodeSession;

    public EpisodeIdQueryTests()
    {
        _fixture = new Fixture();

        _getEpisodeSession = Substitute.For<IGetEpisodeSession>();
        var logger = Substitute.For<ILogger<EpisodeIdQueryHandler>>();
        _sut = new EpisodeIdQueryHandler(_getEpisodeSession, logger);
    }

    [Fact]
    public async Task ReturnsId_WhenRequestingId_GivenValidData()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonId = _fixture.Create<string>();
        var episodeIdentifier = _fixture.Create<string>();
        
        _getEpisodeSession
            .GetEpisodeIdAsync(titleId, seasonId, episodeIdentifier)
            .Returns(_fixture.Create<EpisodeIdResult>());
        
        //Act
        var query = new EpisodeIdQuery(titleId, seasonId, episodeIdentifier);
        var result = await _sut.Handle(query, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        
        await _getEpisodeSession
            .Received(1)
            .GetEpisodeIdAsync(titleId, seasonId, episodeIdentifier);
    }

    [Fact]
    public async Task ReturnsNull_WhenNotFound_GivenNotExistingEpisodeId()
    {
        //Arrange
        var titleId = _fixture.Create<string>();
        var seasonId = _fixture.Create<string>();
        var episodeIdentifier = _fixture.Create<string>();
        
        _getEpisodeSession
            .GetEpisodeIdAsync(titleId, seasonId, episodeIdentifier)
            .Returns((EpisodeIdResult?)null);
        
        //Act
        var query = new EpisodeIdQuery(titleId, seasonId, episodeIdentifier);
        var result = await _sut.Handle(query, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        await _getEpisodeSession
            .Received(1)
            .GetEpisodeIdAsync(titleId, seasonId, episodeIdentifier);
    }
}