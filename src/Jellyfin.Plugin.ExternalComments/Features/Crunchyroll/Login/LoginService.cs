using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login.Client;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;

public class LoginService : ILoginService
{
    private readonly ICrunchyrollLoginClient _crunchyrollClient;
    private readonly ILogger<LoginService> _logger;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public LoginService(ICrunchyrollLoginClient crunchyrollClient, ILogger<LoginService> logger,
        ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _crunchyrollClient = crunchyrollClient;
        _logger = logger;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
    }
    
    public async ValueTask<Result> LoginAnonymously(CancellationToken cancellationToken)
    {
        var currentSession = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(currentSession))
        {
            return Result.Ok();
        }
        
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