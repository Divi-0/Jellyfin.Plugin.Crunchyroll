using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public class EpisodeIdQueryTests
{
    private readonly Fixture _fixture;
    
    private readonly EpisodeIdQueryHandler _sut;
    private readonly IGetEpisodeSession _getEpisodeSession;

    public EpisodeIdQueryTests()
    {
        _fixture = new Fixture();

        _getEpisodeSession = Substitute.For<IGetEpisodeSession>();
        _sut = new EpisodeIdQueryHandler(_getEpisodeSession);
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
            .Returns(_fixture.Create<string>());
        
        //Act
        var query = new EpisodeIdQuery(titleId, seasonId, episodeIdentifier);
        var id = await _sut.Handle(query, CancellationToken.None);
        
        //Assert
        id.Should().NotBeEmpty();
        
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
            .Returns((string?)null);
        
        //Act
        var query = new EpisodeIdQuery(titleId, seasonId, episodeIdentifier);
        var id = await _sut.Handle(query, CancellationToken.None);
        
        //Assert
        id.Should().BeNull();
        
        await _getEpisodeSession
            .Received(1)
            .GetEpisodeIdAsync(titleId, seasonId, episodeIdentifier);
    }
}