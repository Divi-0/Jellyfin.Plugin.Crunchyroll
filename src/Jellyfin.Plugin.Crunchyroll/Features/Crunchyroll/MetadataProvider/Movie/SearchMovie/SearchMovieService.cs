using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.Crunchyroll.SearchDto;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.SearchMovie;

/// <summary>
/// Movies on crunchyroll are episodes of a movie listing, so the search is done for episodes.
/// </summary>
public class SearchMovieService : ISearchMovieService
{
    private const string SearchType = "episode";

    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSearchClient _searchClient;

    public SearchMovieService(ILoginService loginService, ICrunchyrollSearchClient searchClient)
    {
        _loginService = loginService;
        _searchClient = searchClient;
    }

    public async Task<Result<IReadOnlyList<RemoteSearchResult>>> SearchAsync(MovieInfo searchInfo,
        CancellationToken cancellationToken)
    {
        var seriesId = searchInfo.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);
        var seasonId = searchInfo.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeasonId);
        var episodeId = searchInfo.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (!string.IsNullOrWhiteSpace(seriesId) &&
            !string.IsNullOrWhiteSpace(seasonId) &&
            !string.IsNullOrWhiteSpace(episodeId))
        {
            return new List<RemoteSearchResult>
            {
                new()
                {
                    Name = searchInfo.Name,
                    ProductionYear = searchInfo.Year,
                    SearchProviderName = CrunchyrollExternalKeys.ProviderName,
                    ProviderIds =
                    {
                        { CrunchyrollExternalKeys.SeriesId, seriesId },
                        { CrunchyrollExternalKeys.SeasonId, seasonId },
                        { CrunchyrollExternalKeys.EpisodeId, episodeId }
                    }
                }
            };
        }

        if (string.IsNullOrWhiteSpace(searchInfo.Name))
        {
            return Result.Ok<IReadOnlyList<RemoteSearchResult>>([]);
        }

        var loginResult = await _loginService.LoginAnonymouslyAsync(cancellationToken);

        if (loginResult.IsFailed)
        {
            return loginResult;
        }

        var searchResult = await _searchClient.SearchAsync(searchInfo.Name, SearchType,
            searchInfo.GetPreferredMetadataCultureInfo(), cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }

        return searchResult.Value
            .Where(x => x.EpisodeMetadata is not null)
            .Select(MapToRemoteSearchResult)
            .ToList();
    }

    private static RemoteSearchResult MapToRemoteSearchResult(CrunchyrollSearchDataItem item)
    {
        var episodeMetadata = item.EpisodeMetadata!;

        return new RemoteSearchResult
        {
            Name = item.Title,
            Overview = item.Description,
            PremiereDate = episodeMetadata.EpisodeAirDate,
            ProductionYear = episodeMetadata.EpisodeAirDate?.Year,
            ImageUrl = item.Images?.Thumbnail.GetLargestImageSource(),
            SearchProviderName = CrunchyrollExternalKeys.ProviderName,
            ProviderIds =
            {
                { CrunchyrollExternalKeys.SeriesId, episodeMetadata.SeriesId },
                { CrunchyrollExternalKeys.SeasonId, episodeMetadata.SeasonId },
                { CrunchyrollExternalKeys.EpisodeId, item.Id }
            }
        };
    }
}