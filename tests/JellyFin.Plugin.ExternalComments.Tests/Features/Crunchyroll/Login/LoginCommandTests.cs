using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.Login;

public class LoginCommandTests
{
    private readonly ILogger<LoginCommandHandler> _loggerMock;
    private readonly ICrunchyrollSessionRepository _sessionRepositoryMock;
    private readonly ICrunchyrollLoginClient _crunchyrollClientMock;

    public LoginCommandTests()
    {
        _loggerMock = Substitute.For<ILogger<LoginCommandHandler>>();
        _sessionRepositoryMock = Substitute.For<ICrunchyrollSessionRepository>();
        _crunchyrollClientMock = Substitute.For<ICrunchyrollLoginClient>();
    }

    [Fact]
    public async Task ReturnsSuccess_WhenTryingToLogin_GivenLoginCommand()
    {
        _crunchyrollClientMock.LoginAnonymousAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new CrunchyrollAuthResponse()
            {
                AccessToken = "token",
                ExpiresIn = 3600,
                TokenType = "Bearer",
                Scope = "*",
                Country = "de-DE"
            }));

        var loginCommandHandler = new LoginCommandHandler(_crunchyrollClientMock, _loggerMock, _sessionRepositoryMock);

        var result = await loginCommandHandler.Handle(new LoginCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnsSuccessAndSetsSession_WhenTryingToLogin_GivenLoginCommand()
    {
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

        var loginCommandHandler = new LoginCommandHandler(_crunchyrollClientMock, _loggerMock, _sessionRepositoryMock);

        var result = await loginCommandHandler.Handle(new LoginCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _sessionRepositoryMock
            .Received(1)
            .SetAsync(authResponse.AccessToken, TimeSpan.FromSeconds(authResponse.ExpiresIn));
    }

    [Fact]
    public async Task ReturnsFailedAndSessionNotSet_WhenCrunchyrollRequestFails_GivenLoginCommand()
    {
        const string errorMessage = "Error 123";

        _crunchyrollClientMock.LoginAnonymousAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(errorMessage));

        var loginCommandHandler = new LoginCommandHandler(_crunchyrollClientMock, _loggerMock, _sessionRepositoryMock);

        var result = await loginCommandHandler.Handle(new LoginCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.First().Message.Should().Be(errorMessage);

        await _sessionRepositoryMock
            .Received(0)
            .SetAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>());
    }
}