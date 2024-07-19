using System.Security.Authentication;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Mediator;
using NSubstitute.ExceptionExtensions;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.Login;

public class LoginPipelineBehaviorTests
{
    private readonly LoginPipelineBehavior<ICrunchyrollCommand, ResultBase> _sut;
    private readonly IMediator _mediator;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;
    
    public LoginPipelineBehaviorTests()
    {
        _mediator = Substitute.For<IMediator>();
        _crunchyrollSessionRepository = Substitute.For<ICrunchyrollSessionRepository>();

        _sut = new LoginPipelineBehavior<ICrunchyrollCommand, ResultBase>(_mediator, _crunchyrollSessionRepository);
    }

    [Fact]
    public async Task ReturnsSuccess_WhenHandlingNext_GivenSuccessfulNext()
    {
        //Arrange
        var message = Substitute.For<ICrunchyrollCommand>();
        var next = new MessageHandlerDelegate<ICrunchyrollCommand, ResultBase>((_, _) => ValueTask.FromResult((ResultBase)Result.Ok()));

        _crunchyrollSessionRepository
            .GetAsync(Arg.Any<CancellationToken>())!
            .Returns(ValueTask.FromResult("token"));
        
        //Act
        var result = await _sut.Handle(message, CancellationToken.None, next);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public Task ThrowsException_WhenLoginFails_GivenMessage()
    {
        //Arrange
        var message = Substitute.For<ICrunchyrollCommand>();
        var next = new MessageHandlerDelegate<ICrunchyrollCommand, ResultBase>((_, _) => ValueTask.FromResult((ResultBase)Result.Ok()));

        _mediator
            .Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Result.Fail("error")));
        
        //Act & Assert
        var action = async () => 
            await _sut.Handle(message, CancellationToken.None, next);
        
        action
            .Should()
            .ThrowAsync<AuthenticationException>()
            .WithMessage("error");

        return Task.CompletedTask;
    }
}