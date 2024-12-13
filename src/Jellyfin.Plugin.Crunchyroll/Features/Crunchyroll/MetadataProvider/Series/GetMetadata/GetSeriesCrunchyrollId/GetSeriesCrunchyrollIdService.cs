using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.GetSeriesCrunchyrollId.Client;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.GetSeriesCrunchyrollId;

public class GetSeriesCrunchyrollIdService : IGetSeriesCrunchyrollIdService
{
    private readonly ICrunchyrollSeriesIdClient _client;
    private readonly ILoginService _loginService;

    public GetSeriesCrunchyrollIdService(ICrunchyrollSeriesIdClient client,
        ILoginService loginService)
    {
        _client = client;
        _loginService = loginService;
    }
    
    public async Task<Result<CrunchyrollId?>> GetSeriesCrunchyrollId(string name, CultureInfo language, CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }
        
        var titleIdResult = await _client.GetSeriesIdAsync(name, language, cancellationToken);

        return titleIdResult.IsSuccess 
            ? titleIdResult.Value 
            : titleIdResult;
    }
}