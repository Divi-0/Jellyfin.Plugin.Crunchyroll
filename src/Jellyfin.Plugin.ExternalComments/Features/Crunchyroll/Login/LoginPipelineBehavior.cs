using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;

public class LoginPipelineBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse> where TMessage : ICrunchyrollCommand
{
    private readonly IMediator _mediator;
    private readonly ICrunchyrollSessionRepository _crunchyrollSessionRepository;

    public LoginPipelineBehavior(IMediator mediator, ICrunchyrollSessionRepository crunchyrollSessionRepository)
    {
        _mediator = mediator;
        _crunchyrollSessionRepository = crunchyrollSessionRepository;
    }
    
    public async ValueTask<TResponse> Handle(TMessage message, CancellationToken cancellationToken, MessageHandlerDelegate<TMessage, TResponse> next)
    {
        var token = await _crunchyrollSessionRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            var result = await _mediator.Send(new LoginCommand(), cancellationToken);

            if (result.IsFailed)
            {
                throw new AuthenticationException(result.Errors.First().Message);
            }
        }
        
        return await next(message, cancellationToken);
    }
}