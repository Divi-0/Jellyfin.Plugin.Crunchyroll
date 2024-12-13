using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId.Client;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;

public class GetMovieCrunchyrollIdService : IGetMovieCrunchyrollIdService
{
    private readonly ICrunchyrollMovieEpisodeIdClient _client;

    public GetMovieCrunchyrollIdService(ICrunchyrollMovieEpisodeIdClient client)
    {
        _client = client;
    }
    
    public async Task<Result<MovieCrunchyrollIdResult?>> GetCrunchyrollIdAsync(string fileName, 
        CultureInfo language, CancellationToken cancellationToken)
    {
        var searchResult = await _client.SearchTitleIdAsync(fileName, language, cancellationToken);

        if (searchResult.IsFailed)
        {
            return searchResult.ToResult();
        }
        
        return searchResult.Value is null
            ? null
            : MapToMovieCrunchyrollIdResult(searchResult.Value);
    }

    private static MovieCrunchyrollIdResult MapToMovieCrunchyrollIdResult(SearchResponse searchResponse)
    {
        return new MovieCrunchyrollIdResult
        {
            SeriesId = searchResponse.SeriesId,
            SeasonId = searchResponse.SeasonId,
            EpisodeId = searchResponse.EpisodeId
        };
    }
}