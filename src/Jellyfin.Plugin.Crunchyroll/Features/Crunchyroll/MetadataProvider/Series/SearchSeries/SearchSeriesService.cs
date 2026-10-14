using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SearchSeries;

public class SearchSeriesService : ISearchSeriesService
{
    private const string SearchType = "series";

    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSearchClient _searchClient;
    private readonly ICrunchyrollSeriesClient _seriesClient;

    public SearchSeriesService(ILoginService loginService, ICrunchyrollSearchClient searchClient,
        ICrunchyrollSeriesClient seriesClient)
    {
        _loginService = loginService;
        _searchClient = searchClient;
        _seriesClient = seriesClient;
    }

    public async Task<Result<IReadOnlyList<RemoteSearchResult>>> SearchAsync(SeriesInfo searchInfo,
        CancellationToken cancellationToken)
    {
        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }

        var language = searchInfo.GetPreferredMetadataCultureInfo();

        if (searchInfo.ProviderIds.TryGetValue(CrunchyrollExternalKeys.SeriesId, out var seriesId) &&
            !string.IsNullOrWhiteSpace(seriesId))
        {
            return await GetBySeriesIdAsync(seriesId, language, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(searchInfo.Name))
        {
            return Result.Ok<IReadOnlyList<RemoteSearchResult>>([]);
        }

        var searchResult = await _searchClient.SearchAsync(searchInfo.Name, SearchType, language, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        return searchResult.Value
            .Select(MapToRemoteSearchResult)
            .ToList();
    }

    private async Task<Result<IReadOnlyList<RemoteSearchResult>>> GetBySeriesIdAsync(string seriesId,
        CultureInfo language, CancellationToken cancellationToken)
    {
        var seriesResult = await _seriesClient.GetSeriesMetadataAsync(seriesId, language, cancellationToken);

        if (seriesResult.IsFailed)
        {
            return seriesResult.ToResult();
        }

        var series = seriesResult.Value;

        return new List<RemoteSearchResult>
        {
            new()
            {
                Name = series.Title,
                Overview = series.Description,
                ImageUrl = series.Images.PosterTall
                    .SelectMany(x => x)
                    .MaxBy(x => x.Width)?
                    .Source,
                SearchProviderName = CrunchyrollExternalKeys.ProviderName,
                ProviderIds = { { CrunchyrollExternalKeys.SeriesId, series.Id } }
            }
        };
    }

    private static RemoteSearchResult MapToRemoteSearchResult(CrunchyrollSearchDataItem item)
    {
        return new RemoteSearchResult
        {
            Name = item.Title,
            Overview = item.Description,
            ProductionYear = item.SeriesMetadata?.SeriesLaunchYear,
            ImageUrl = item.Images?.PosterTall.GetLargestImageSource(),
            SearchProviderName = CrunchyrollExternalKeys.ProviderName,
            ProviderIds = { { CrunchyrollExternalKeys.SeriesId, item.Id } }
        };
    }
}