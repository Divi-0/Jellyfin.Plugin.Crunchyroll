using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login.Client;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;

public record LoginCommand : IRequest<Result>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result>
{
    private readonly ICrunchyrollLoginClient _crunchyrollClient;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public LoginCommandHandler(ICrunchyrollLoginClient crunchyrollClient, ILogger<LoginCommandHandler> logger,
        ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _crunchyrollClient = crunchyrollClient;
        _logger = logger;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
    }

    public async ValueTask<Result> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginAnonymousResponse = await _crunchyrollClient.LoginAnonymousAsync(cancellationToken);

        if (loginAnonymousResponse.IsFailed)
        {
            return Result.Fail(loginAnonymousResponse.Errors);
        }

        await _crunchyrollSessionRepository.SetAsync(loginAnonymousResponse.Value.AccessToken,
            TimeSpan.FromSeconds(loginAnonymousResponse.Value.ExpiresIn), cancellationToken);

        _logger.LogInformation("Anonymous Crunchyroll login was successful");

        return Result.Ok();
    }
}