namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common.Helpers;

public static class CreateVideoFolderHelper
{
    public static void CreateVideoFolder(string basePath)
    {
        Directory.CreateDirectory(basePath);
        
        //CreateOnePieceFolder(basePath);
        CreateSwordArtOnlineFolder(basePath);
        CreateAttackOnTitan(basePath);
        CreateThatTimeIGotReincarnatedAsASlime(basePath);
        CreateBlueExorcist(basePath);
        CreateJoJosBizarreAdventure(basePath);
        CreateRurouniKenshin(basePath);
        CreateCardfightVanguardOverDress(basePath);
        CreateLaidBackCamp(basePath);
    }

    private static DirectoryInfo CreateSeasonsFolderWithEpisodes(string seriesPath, int seasonNumber, int nextEpisodeNumber, 
        int episodeCount, bool isDuplicateSeason = false)
    {
        var seasonDirectory = Directory.CreateDirectory(Path.Combine(seriesPath, $"Season {seasonNumber}{(isDuplicateSeason ? " - 2" : string.Empty)}"));

        var currentEpisodeNumber = nextEpisodeNumber;
        var totalEpisodes = nextEpisodeNumber + episodeCount - 1;
        
        for (; currentEpisodeNumber <= totalEpisodes; currentEpisodeNumber++)
        {
            File.WriteAllBytesAsync(Path.Combine(seasonDirectory.FullName, $"S{seasonNumber:00}E{currentEpisodeNumber:0000}.mp4"), []);
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
            CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 14, nextEpisodeNumber: 1089, episodeCount: 33);
        
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
    
    private static void CreateSwordArtOnlineFolder(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Sword Art Online"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 25);
        var seasonTwoDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 1, episodeCount: 24);
        File.WriteAllBytesAsync(Path.Combine(seasonTwoDirectory.FullName, "S02E14.5.mp4"), []);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 1, episodeCount: 24);
            
        var alicizationWarOfUnderworldDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Alicization War of Underworld"));
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E05.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E06.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E07.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E08.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E09.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E10.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E11.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E12.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E13.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E14.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E15.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E16.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E17.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E18.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E19.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E20.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E21.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E22.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(alicizationWarOfUnderworldDirectory.FullName, "E23.mp4"), []);
            
        var movieOrdinalScaleDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Sword Art Online the Movie -Ordinal Scale-"));
        File.WriteAllBytesAsync(Path.Combine(movieOrdinalScaleDirectory.FullName, "Ordinal Scale.mp4"), []);
            
        var movieAriaOfAStarlessNightDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Sword Art Online the Movie -Progressive- Aria of a Starless Night"));
        File.WriteAllBytesAsync(Path.Combine(movieAriaOfAStarlessNightDirectory.FullName, "Sword Art Online the Movie -Progressive- Aria of a Starless Night.mp4"), []);
            
        var movieScherzoOfDeepNightDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Sword Art Online the Movie -Progressive- Scherzo of Deep Night"));
        File.WriteAllBytesAsync(Path.Combine(movieScherzoOfDeepNightDirectory.FullName, "Sword Art Online the Movie -Progressive- Scherzo.mp4"), []);
    }
    
    private static void CreateAttackOnTitan(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Attack on Titan"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 26, episodeCount: 12);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 38, episodeCount: 22);
        var seasonFourDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 4, nextEpisodeNumber: 60, episodeCount: 28);
        File.WriteAllBytesAsync(Path.Combine(seasonFourDirectory.FullName, "S04E-SP1.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourDirectory.FullName, "S04E-SP2.mp4"), []);
        
        var oadsDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Attack on Titan OADs"));
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E05.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E06.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E07.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E08.mp4"), []);
    }
    
    private static void CreateThatTimeIGotReincarnatedAsASlime(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "That Time I Got Reincarnated As A Slime"));
        
        var seasonOneDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 24);
        File.WriteAllBytesAsync(Path.Combine(seasonOneDirectory.FullName, "S01E0024.5.mp4"), []);
        
        var seasonTwoDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 25, episodeCount: 24);
        File.WriteAllBytesAsync(Path.Combine(seasonTwoDirectory.FullName, "S02E0024.9.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonTwoDirectory.FullName, "S02E0036.5.mp4"), []);
        
        var seasonThreeDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 49, episodeCount: 24);
        File.WriteAllBytesAsync(Path.Combine(seasonThreeDirectory.FullName, "S03E0048.5.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThreeDirectory.FullName, "S03E0065.5.mp4"), []);
        
        var oadsDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "That Time I Got Reincarnated as a Slime OAD"));
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(oadsDirectory.FullName, "E05.mp4"), []);
        
        var movieDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "That Time I Got Reincarnated as a Slime the Movie Scarlet Bond"));
        File.WriteAllBytesAsync(Path.Combine(movieDirectory.FullName, "That Time I Got Reincarnated as a Slime the Movie Scarlet Bond.mp4"), []);
        
        var visionsOfColeusDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "That Time I Got Reincarnated as a Slime Visions of Coleus"));
        File.WriteAllBytesAsync(Path.Combine(visionsOfColeusDirectory.FullName, "E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(visionsOfColeusDirectory.FullName, "E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(visionsOfColeusDirectory.FullName, "E03.mp4"), []);
    }
    
    private static void CreateBlueExorcist(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Blue Exorcist"));
        
        var shimaneIlluminatiSagaDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Season 3 - Shimane Illuminati Saga"));
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E05.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E06.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E07.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E08.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E09.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E10.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E11.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(shimaneIlluminatiSagaDirectory.FullName, "S03E12.mp4"), []);
        
        var beyondTheSnowSagaDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Season 3 - Beyond the Snow Saga"));
        File.WriteAllBytesAsync(Path.Combine(beyondTheSnowSagaDirectory.FullName, "S03E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(beyondTheSnowSagaDirectory.FullName, "S03E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(beyondTheSnowSagaDirectory.FullName, "S03E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(beyondTheSnowSagaDirectory.FullName, "S03E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(beyondTheSnowSagaDirectory.FullName, "S03E05.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(beyondTheSnowSagaDirectory.FullName, "S03E06.mp4"), []);
    }
    
    private static void CreateJoJosBizarreAdventure(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "JoJo's Bizarre Adventure"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 26);
        var seasonOneReEditedDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Season 1 - Re-Edited"));
        File.WriteAllBytesAsync(Path.Combine(seasonOneReEditedDirectory.FullName, "S01E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonOneReEditedDirectory.FullName, "S01E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonOneReEditedDirectory.FullName, "S01E03.mp4"), []);
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 1, episodeCount: 48);
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 1, episodeCount: 39);
        var seasonFourDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 4, nextEpisodeNumber: 1, episodeCount: 39);
        File.WriteAllBytesAsync(Path.Combine(seasonFourDirectory.FullName, "S04E0013.5.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourDirectory.FullName, "S04E0021.5.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonFourDirectory.FullName, "S04E0028.5.mp4"), []);
    }
    
    private static void CreateRurouniKenshin(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Rurouni Kenshin"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 24);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 25, episodeCount: 7);
    }
    
    private static void CreateCardfightVanguardOverDress(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "CARDFIGHT!! VANGUARD overDress"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 25);

        var willDressSeasonOneDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Season 1 - CARDFIGHT!! VANGUARD will+Dress"));
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E05.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E06.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E07.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E08.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E09.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E10.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E11.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E12.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(willDressSeasonOneDirectory.FullName, "E13.mp4"), []);
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 1, episodeCount: 13);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 1, episodeCount: 13);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 4, nextEpisodeNumber: 1, episodeCount: 13);
        
        var divinezSeasonTwoDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "CARDFIGHT!! VANGUARD Divinez Season 2"));
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E01.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E02.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E03.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E04.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E05.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E06.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E07.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E08.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E09.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E10.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E11.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E12.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(divinezSeasonTwoDirectory.FullName, "E13.mp4"), []);
    }
    
    private static void CreateLaidBackCamp(string basePath)
    {
        var seriesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Laid-Back Camp"));
        
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 1, nextEpisodeNumber: 1, episodeCount: 12);
        CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 2, nextEpisodeNumber: 1, episodeCount: 13);
        var seasonThreeDirectory = CreateSeasonsFolderWithEpisodes(seriesDirectory.FullName, seasonNumber: 3, nextEpisodeNumber: 1, episodeCount: 12);
        File.WriteAllBytesAsync(Path.Combine(seasonThreeDirectory.FullName, "S03E13A.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThreeDirectory.FullName, "S03E13B.mp4"), []);
        File.WriteAllBytesAsync(Path.Combine(seasonThreeDirectory.FullName, "S03E13C.mp4"), []);
        
        var movieDirectory = Directory.CreateDirectory(Path.Combine(seriesDirectory.FullName, "Laid-Back Camp Movie"));
        File.WriteAllBytesAsync(Path.Combine(movieDirectory.FullName, "Laid-Back Camp Movie.mp4"), []);
    }
}