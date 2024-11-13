namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;

public static class CreateVideoFolderHelper
{
    public static void CreateVideoFolder(string basePath)
    {
        Directory.CreateDirectory(basePath);
        
        CreateOnePieceFolder(basePath);
    }

    private static DirectoryInfo CreateSeasonsFolderWithEpisodes(string seriesPath, int seasonNumber, int nextEpisodeNumber, 
        int episodeCount, bool isDuplicateSeason = false)
    {
        var seasonDirectory = Directory.CreateDirectory(Path.Combine(seriesPath, $"Season {seasonNumber}{(isDuplicateSeason ? " - 2" : string.Empty)}"));

        var currentEpisodeNumber = nextEpisodeNumber;
        var totalEpisodes = nextEpisodeNumber + episodeCount - 1;
        
        for (; currentEpisodeNumber <= totalEpisodes; currentEpisodeNumber++)
        {
            File.WriteAllBytesAsync(Path.Combine(seasonDirectory.FullName, $"S{seasonNumber}E{currentEpisodeNumber:0000}.mp4"), []);
        }

        return seasonDirectory;
    }
    
    private static void CreateOnePieceFolder(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "One Piece"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 61);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 62, episodeCount: 74);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 136, episodeCount: 71);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 4, nextEpisodeNumber: 207, episodeCount: 119);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 5, nextEpisodeNumber: 326, episodeCount: 59);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 6, nextEpisodeNumber: 385, episodeCount: 131);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 7, nextEpisodeNumber: 517, episodeCount: 57);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 8, nextEpisodeNumber: 575, episodeCount: 54);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 9, nextEpisodeNumber: 630, episodeCount: 70);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 9, nextEpisodeNumber: 700, episodeCount: 47,
            isDuplicateSeason: true);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 10, nextEpisodeNumber: 747, episodeCount: 4);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 10, nextEpisodeNumber: 751, episodeCount: 32,
            isDuplicateSeason: true);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 11, nextEpisodeNumber: 783, episodeCount: 96);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 12, nextEpisodeNumber: 879, episodeCount: 13);
        var seasonThirteenDir = 
            CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 13, nextEpisodeNumber: 892, episodeCount: 197);
        
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP - Special Episode Barto's Secret Room.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP2 - A Special Episode to Admire Zoro-senpai and Sanji-senpai! Barto's Secret Room 2!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP3 - A Comprehensive Anatomy! The Legend of Kozuki Oden!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP4 - The Captain’s Log of the Legend! Red-Haired Shanks!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP5 - A Comprehensive Anatomy! Fierce Fight! The Five from the New Generation.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP6 - Recapping Fierce Fights! Straw Hats vs. Tobi Roppo.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP7 - Recapping Fierce Fights! Zoro vs. A Lead Performer!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP8 - Recapping Fierce Fights! The Countercharge Alliance vs. Big Mom.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP9 - Luffy-senpai Support Project! Barto's Secret Room 3!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP10 - Luffy-senpai Support Project! Barto's Secret Room 4!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThirteenDir.FullName, "S13E-SP11 - A Very Special Feature! Momonosuke's Road to Becoming a Great Shogun.mp4"), []);
        
        var seasonFourteenDir = 
            CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 14, nextEpisodeNumber: 1089, episodeCount: 39);
        
        File.WriteAllBytesAsync(Path.Combine(seasonFourteenDir.FullName, "S14E-SP12 - A Project to Fully Enjoy! ‘Surgeon of Death’ Trafalgar Law.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourteenDir.FullName, "S14E-SP13 - The Log of the Rivalry! The Straw Hats vs. Cipher Pol.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourteenDir.FullName, "S14E-SP14 - Making History! The Turbulent Old and New Four Emperors!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourteenDir.FullName, "S14E-SP15 - The Log of the Turbulent Revolution! The Revolutionary Army Maneuvers in Secret!.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourteenDir.FullName, "S14E-SP16 - Unwavering Justice! The Navy's Proud Log!.mp4"), []);
        
        var onePieceFanLetterSeasonDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "ONE PIECE FAN LETTER"));
        File.WriteAllBytesAsync(Path.Combine(onePieceFanLetterSeasonDirectory.FullName, "E1124 - Special ONE PIECE FAN LETTER.mp4"), []);
        
        var onePieceFishManIslandSagaSeasonDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "One Piece Log Fish-Man Island Saga (Current)"));
        File.WriteAllBytesAsync(Path.Combine(onePieceFishManIslandSagaSeasonDirectory.FullName, "E-FMI1 - Abc.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(onePieceFishManIslandSagaSeasonDirectory.FullName, "E-FMI2 - Def.mp4"), []);
    }
}