namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;

public static class CreateVideoFolderHelper
{
    public static void CreateVideoFolder(string basePath)
    {
        Directory.CreateDirectory(basePath);
        
        CreateOnePieceFolder(basePath);
    }

    private static void CreateSeasonsFolderWithEpisodes(string seriesPath, int seasonNumber, int nextEpisodeNumber, 
        int episodeCount, bool isDuplicateSeason = false)
    {
        var seasonDirectory = Directory.CreateDirectory(Path.Combine(seriesPath, $"Season {seasonNumber}{(isDuplicateSeason ? " - 2" : string.Empty)}"));

        var currentEpisodeNumber = nextEpisodeNumber;
        var totalEpisodes = nextEpisodeNumber + episodeCount - 1;
        
        for (; currentEpisodeNumber <= totalEpisodes; currentEpisodeNumber++)
        {
            File.WriteAllBytesAsync(Path.Combine(seasonDirectory.FullName, $"S{seasonNumber}E{currentEpisodeNumber:00}.mp4"), []);
        }
    }
    
    private static void CreateOnePieceFolder(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "One Piece"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 61);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 62, episodeCount: 74);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 136, episodeCount: 71);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 9, nextEpisodeNumber: 630, episodeCount: 70);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 9, nextEpisodeNumber: 700, episodeCount: 47,
            isDuplicateSeason: true);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 10, nextEpisodeNumber: 747, episodeCount: 4);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 10, nextEpisodeNumber: 751, episodeCount: 32,
            isDuplicateSeason: true);
        
        
        var onePieceFanLetterSeasonDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "ONE PIECE FAN LETTER"));
        File.WriteAllBytesAsync(Path.Combine(onePieceFanLetterSeasonDirectory.FullName, "E1124.mp4"), []);
    }
}