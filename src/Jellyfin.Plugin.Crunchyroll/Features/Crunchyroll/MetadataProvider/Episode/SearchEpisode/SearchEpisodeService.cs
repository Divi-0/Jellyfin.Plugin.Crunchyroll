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

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.SearchEpisode;

public class SearchEpisodeService : ISearchEpisodeService
{
    private const string SearchType = "episode";

    private readonly ILoginService _loginService;
    private readonly ICrunchyrollSearchClient _searchClient;

    public SearchEpisodeService(ILoginService loginService, ICrunchyrollSearchClient searchClient)
    {
        _loginService = loginService;
        _searchClient = searchClient;
    }

    public async Task<Result<IReadOnlyList<RemoteSearchResult>>> SearchAsync(EpisodeInfo searchInfo,
        CancellationToken cancellationToken)
    {
        var episodeId = searchInfo.ProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.EpisodeId);

        if (!string.IsNullOrWhiteSpace(episodeId))
        {
            return new List<RemoteSearchResult>
            {
                new()
                {
                    Name = searchInfo.Name,
                    IndexNumber = searchInfo.IndexNumber,
                    ParentIndexNumber = searchInfo.ParentIndexNumber,
                    PremiereDate = searchInfo.PremiereDate,
                    ProductionYear = searchInfo.Year,
                    SearchProviderName = CrunchyrollExternalKeys.ProviderName,
                    ProviderIds = { { CrunchyrollExternalKeys.EpisodeId, episodeId } }
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

        var seriesId = searchInfo.SeriesProviderIds.GetValueOrDefault(CrunchyrollExternalKeys.SeriesId);

        //episodes of the series the episode belongs to are listed first; OrderBy is stable, so crunchyroll's
        //relevance order is kept otherwise
        return searchResult.Value
            .Where(x => x.EpisodeMetadata is not null)
            .OrderByDescending(x => !string.IsNullOrWhiteSpace(seriesId) && x.EpisodeMetadata!.SeriesId == seriesId)
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
            IndexNumber = episodeMetadata.EpisodeNumber,
            PremiereDate = episodeMetadata.EpisodeAirDate,
            ProductionYear = episodeMetadata.EpisodeAirDate?.Year,
            ImageUrl = item.Images?.Thumbnail.GetLargestImageSource(),
            SearchProviderName = CrunchyrollExternalKeys.ProviderName,
            ProviderIds = { { CrunchyrollExternalKeys.EpisodeId, item.Id } }
        };
    }
}