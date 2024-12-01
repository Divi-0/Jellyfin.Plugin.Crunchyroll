using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.TitleMetadata.GetEpisodeId;

public class EpisodeIdQueryByNameTests
{
    private readonly Fixture _fixture;
    
    private readonly EpisodeIdQueryHandler _sut;
    private readonly IGetEpisodeRepository _repository;

    public EpisodeIdQueryByNameTests()
    {
        _fixture = new Fixture();

        _repository = Substitute.For<IGetEpisodeRepository>();
        var logger = Substitute.For<ILogger<EpisodeIdQueryHandler>>();
        
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider
            .GetService<IGetEpisodeRepository>()
            .Returns(_repository);

        scopeFactory
            .CreateScope()
            .Returns(scope);
        
        _sut = new EpisodeIdQueryHandler(scopeFactory, logger);
    }

    [Fact]
    public async Task ReturnsId_WhenRequestingId_GivenValidData()
    {
        //Arrange
        var seasonId = _fixture.Create<string>();
        var episodeName = _fixture.Create<string>();
        
        _repository
            .GetEpisodeIdAsync(seasonId, episodeName, Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<EpisodeIdResult>());
        
        //Act
        var query = new EpisodeIdQuery(seasonId, episodeName);
        var result = await _sut.Handle(query, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        
        await _repository
            .Received(1)
            .GetEpisodeIdAsync(seasonId, episodeName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNull_WhenNotFound_GivenNotExistingEpisodeId()
    {
        //Arrange
        var seasonId = _fixture.Create<string>();
        var episodeName = _fixture.Create<string>();
        
        _repository
            .GetEpisodeIdAsync(seasonId, episodeName, Arg.Any<CancellationToken>())
            .Returns((EpisodeIdResult?)null);
        
        //Act
        var query = new EpisodeIdQuery(seasonId, episodeName);
        var result = await _sut.Handle(query, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        
        await _repository
            .Received(1)
            .GetEpisodeIdAsync(seasonId, episodeName, Arg.Any<CancellationToken>());
    }
}