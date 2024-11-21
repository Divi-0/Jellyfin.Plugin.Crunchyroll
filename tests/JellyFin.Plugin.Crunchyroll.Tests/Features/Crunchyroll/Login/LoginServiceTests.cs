using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login.Client;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.Login;

public class LoginServiceTests
{
    private readonly ILogger<LoginService> _loggerMock;
    private readonly ICrunchyrollSessionRepository _sessionRepositoryMock;
    private readonly ICrunchyrollLoginClient _crunchyrollClientMock;
    
    private readonly LoginService _sut;

    public LoginServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<LoginService>>();
        _sessionRepositoryMock = Substitute.For<ICrunchyrollSessionRepository>();
        _crunchyrollClientMock = Substitute.For<ICrunchyrollLoginClient>();
        
        _sut = new LoginService(_crunchyrollClientMock, _loggerMock, _sessionRepositoryMock);
    }

    [Fact]
    public async Task ReturnsSuccessAndSetsSession_WhenTryingToLogin_GivenLoginCommand()
    {
        //Arrange
        _sessionRepositoryMock
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        var authResponse = new CrunchyrollAuthResponse()
        {
            AccessToken = "token",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            Scope = "*",
            Country = "de-DE"
        };

        _crunchyrollClientMock.LoginAnonymousAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok(authResponse));

        //Act
        var result = await _sut.LoginAnonymouslyAsync(CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _sessionRepositoryMock
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());

        await _sessionRepositoryMock
            .Received(1)
            .SetAsync(authResponse.AccessToken, TimeSpan.FromSeconds(authResponse.ExpiresIn));
    }

    [Fact]
    public async Task ReturnsFailedAndSessionNotSet_WhenCrunchyrollRequestFails_GivenLoginCommand()
    {
        //Arrange
        const string errorMessage = "Error 123";

        _sessionRepositoryMock
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns((string?)null);
        
        _crunchyrollClientMock.LoginAnonymousAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(errorMessage));

        //Act
        var result = await _sut.LoginAnonymouslyAsync(CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(errorMessage);
        
        await _sessionRepositoryMock
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());

        await _sessionRepositoryMock
            .Received(0)
            .SetAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task ReturnsSuccess_WhenSessionAlreadySet_GivenLoginCommand()
    {
        //Arrange
        _sessionRepositoryMock
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid().ToString());

        //Act
        var result = await _sut.LoginAnonymouslyAsync(CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        
        await _sessionRepositoryMock
            .Received(1)
            .GetAsync(Arg.Any<CancellationToken>());

        await _crunchyrollClientMock
            .DidNotReceive()
            .LoginAnonymousAsync(Arg.Any<CancellationToken>());

        await _sessionRepositoryMock
            .DidNotReceive()
            .SetAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>());
    }
}