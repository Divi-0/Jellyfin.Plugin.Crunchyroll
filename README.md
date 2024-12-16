## About

DO NOT USE THE PLUGIN IN VERION 1.x.x, I work on version 2.0.0

This plugin is a third party plugin and was not created by the official Jellyfin team.
It collects metadata from Crunchyroll and sets Description, Images, ... to the existing Jellyfin Items.
It also brings back reviews and comments, in a read-only mode. (Scraped from WaybackMachine)

Disclaimer: I could not test every anime because Crunchyroll is region locked. So please take this into consideration, 
some animes might not be recognized correctly. 
If you find any bugs you are welcome to create an issue.

## Features
Metadata that will be fetched from Crunchyroll
- Series
  - Title/Name
  - Description
  - Studio
  - Ratings
  - Background Image
  - Cover Image
  - Reviews via WayBackMachine
- Seasons
  - Title
  - Order (Seasons will be ordered by Crunchyroll listing)
- Episodes
  - Title/Name
  - Description
  - Thumbnail Image
  - Comments via WayBackMachine
- Movies
  - Title/Name
  - Description
  - Studio
  - Reviews via WayBackMachine 
  - Thumbnail Image
  - Comments via WayBackMachine

More details of the features can be found in the wiki [Features](https://github.com/Divi-0/Jellyfin.Plugin.Crunchyroll/wiki/Features) <br>

## Installation
#### Steps
1. In the Jellyfin Dashboard select the `Plugins -> Repositories` Tab and add the manifest `https://raw.githubusercontent.com/Divi-0/Jellyfin.Plugin.Crunchyroll/refs/heads/main/manifest.json`
2. Go to the `Catalog` and install `"Crunchyroll"`
3. Go to the configuration page of the "Crunchyroll" plugin `Plugins -> My Plugins -> Crunchyroll`
4. Enter the name of your anime jellyfin collection/library you want to scan (Example: `Anime`)
5. Enable all the features you would like to have, in the "Features" section
6. Change your folder structure to reflect the structure of Crunchyroll [Usage](#Usage)
7. Restart Jellyfin-Server if you made some changes to the config
8. Done. Run a library scan

## Usage
The Plugin runs as a post task on a library scan, so to get the metadata from Crunchyroll just run a library scan. 
The first scan can take multiple minutes or even hours, depending on your collection/library size.
You can enable debug logging in Jellyfin to see the logs.

### Folder structure
Crunchyroll has some clusterfuck season-numbers, some are correct, some are completely off, almost every big anime is different... To assign the Crunchyroll series and episodes to the Jellyfin items, the folder structure must be adapted to the Crunchyroll structure.
There are some rules that must be followed.

**_Seasons with the same season-number and continuous episode numbers_** <br>
Some Seasons have the same season-number, in that case create a duplicate folder for the same season.
Here is an example for `One Piece` (Notice: Swort Art Online: `S3: Sword Art Online Alicization` & `S3: Sword Art Online Alicization War of Underworld` do not match this rule, so don't create duplicate season folders for them)
```
One Piece/
├─ Season .../
├─ Season 9 - (630-699)/
├─ Season 9 - (700-746)/ (Change the foldername slightly, but so that Jellyfin still recognizes it as the same season)
├─ Season .../
```


**_Season without a season-number_** <br>
Some series have seasons without a season-number, like `Attack on Titan` or `One Piece`.
In that case the `Season`-folder has to be named to the exact Crunchyroll-Season-Name.

Here is an example for `One Piece` & `Attack on Titan`
```
One Piece/
├─ Season 1/
│  ├─ S01E0001
│  ├─ S01E0002
├─ Season 2/
├─ Season .../
├─ ONE PIECE FAN LETTER/
│  ├─ E1124
```

```
Attack on Titan/
├─ Season 1/
│  ├─ S01E01
│  ├─ S01E02
├─ Season 2/
├─ Season .../
├─ Attack on Titan OADs/
│  ├─ E01
```

**_Episodes with decimals/specials between normal episodes_** <br>
If an episode has a decimal number like `E1.5` or a letter `E13A`, just add it to the episode filename. <br>
The plugin will detect it and if it can be matched to a crunchyroll episode, it will automatically be sorted to air
between those episodes.

Example:
```
├─ Season 1/
│  ├─ S01E0001.5
```

```
├─ Season 1/
│  ├─ S01E13A
```

**_Complete Examples_** <br>

<details>
  <summary>One Piece</summary>

```
└───One Piece
    ├───ONE PIECE FAN LETTER
    │       E1124 - Special ONE PIECE FAN LETTER.mp4
    │
    ├───One Piece Log Fish-Man Island Saga (Current)
    │       E-FMI1 - Abc.mp4
    │       E-FMI2 - Def.mp4
    │
    ├───Season 1
    │       S1E0001.mp4
    │       S1E0002.mp4
    │       S1E0003.mp4
    │       S1E0004.mp4
    │       S1E0005.mp4
    │       S1E0006.mp4
    │       S1E0007.mp4
    │       S1E0008.mp4
    │       S1E0009.mp4
    │       S1E0010.mp4
    │       S1E0011.mp4
    │       S1E0012.mp4
    │       S1E0013.mp4
    │       S1E0014.mp4
    │       S1E0015.mp4
    │       S1E0016.mp4
    │       S1E0017.mp4
    │       S1E0018.mp4
    │       S1E0019.mp4
    │       S1E0020.mp4
    │       S1E0021.mp4
    │       S1E0022.mp4
    │       S1E0023.mp4
    │       S1E0024.mp4
    │       S1E0025.mp4
    │       S1E0026.mp4
    │       S1E0027.mp4
    │       S1E0028.mp4
    │       S1E0029.mp4
    │       S1E0030.mp4
    │       S1E0031.mp4
    │       S1E0032.mp4
    │       S1E0033.mp4
    │       S1E0034.mp4
    │       S1E0035.mp4
    │       S1E0036.mp4
    │       S1E0037.mp4
    │       S1E0038.mp4
    │       S1E0039.mp4
    │       S1E0040.mp4
    │       S1E0041.mp4
    │       S1E0042.mp4
    │       S1E0043.mp4
    │       S1E0044.mp4
    │       S1E0045.mp4
    │       S1E0046.mp4
    │       S1E0047.mp4
    │       S1E0048.mp4
    │       S1E0049.mp4
    │       S1E0050.mp4
    │       S1E0051.mp4
    │       S1E0052.mp4
    │       S1E0053.mp4
    │       S1E0054.mp4
    │       S1E0055.mp4
    │       S1E0056.mp4
    │       S1E0057.mp4
    │       S1E0058.mp4
    │       S1E0059.mp4
    │       S1E0060.mp4
    │       S1E0061.mp4
    │
    ├───Season 10
    │       S10E0747.mp4
    │       S10E0748.mp4
    │       S10E0749.mp4
    │       S10E0750.mp4
    │
    ├───Season 10 - 2
    │       S10E0751.mp4
    │       S10E0752.mp4
    │       S10E0753.mp4
    │       S10E0754.mp4
    │       S10E0755.mp4
    │       S10E0756.mp4
    │       S10E0757.mp4
    │       S10E0758.mp4
    │       S10E0759.mp4
    │       S10E0760.mp4
    │       S10E0761.mp4
    │       S10E0762.mp4
    │       S10E0763.mp4
    │       S10E0764.mp4
    │       S10E0765.mp4
    │       S10E0766.mp4
    │       S10E0767.mp4
    │       S10E0768.mp4
    │       S10E0769.mp4
    │       S10E0770.mp4
    │       S10E0771.mp4
    │       S10E0772.mp4
    │       S10E0773.mp4
    │       S10E0774.mp4
    │       S10E0775.mp4
    │       S10E0776.mp4
    │       S10E0777.mp4
    │       S10E0778.mp4
    │       S10E0779.mp4
    │       S10E0780.mp4
    │       S10E0781.mp4
    │       S10E0782.mp4
    │
    ├───Season 11
    │       S11E0783.mp4
    │       S11E0784.mp4
    │       S11E0785.mp4
    │       S11E0786.mp4
    │       S11E0787.mp4
    │       S11E0788.mp4
    │       S11E0789.mp4
    │       S11E0790.mp4
    │       S11E0791.mp4
    │       S11E0792.mp4
    │       S11E0793.mp4
    │       S11E0794.mp4
    │       S11E0795.mp4
    │       S11E0796.mp4
    │       S11E0797.mp4
    │       S11E0798.mp4
    │       S11E0799.mp4
    │       S11E0800.mp4
    │       S11E0801.mp4
    │       S11E0802.mp4
    │       S11E0803.mp4
    │       S11E0804.mp4
    │       S11E0805.mp4
    │       S11E0806.mp4
    │       S11E0807.mp4
    │       S11E0808.mp4
    │       S11E0809.mp4
    │       S11E0810.mp4
    │       S11E0811.mp4
    │       S11E0812.mp4
    │       S11E0813.mp4
    │       S11E0814.mp4
    │       S11E0815.mp4
    │       S11E0816.mp4
    │       S11E0817.mp4
    │       S11E0818.mp4
    │       S11E0819.mp4
    │       S11E0820.mp4
    │       S11E0821.mp4
    │       S11E0822.mp4
    │       S11E0823.mp4
    │       S11E0824.mp4
    │       S11E0825.mp4
    │       S11E0826.mp4
    │       S11E0827.mp4
    │       S11E0828.mp4
    │       S11E0829.mp4
    │       S11E0830.mp4
    │       S11E0831.mp4
    │       S11E0832.mp4
    │       S11E0833.mp4
    │       S11E0834.mp4
    │       S11E0835.mp4
    │       S11E0836.mp4
    │       S11E0837.mp4
    │       S11E0838.mp4
    │       S11E0839.mp4
    │       S11E0840.mp4
    │       S11E0841.mp4
    │       S11E0842.mp4
    │       S11E0843.mp4
    │       S11E0844.mp4
    │       S11E0845.mp4
    │       S11E0846.mp4
    │       S11E0847.mp4
    │       S11E0848.mp4
    │       S11E0849.mp4
    │       S11E0850.mp4
    │       S11E0851.mp4
    │       S11E0852.mp4
    │       S11E0853.mp4
    │       S11E0854.mp4
    │       S11E0855.mp4
    │       S11E0856.mp4
    │       S11E0857.mp4
    │       S11E0858.mp4
    │       S11E0859.mp4
    │       S11E0860.mp4
    │       S11E0861.mp4
    │       S11E0862.mp4
    │       S11E0863.mp4
    │       S11E0864.mp4
    │       S11E0865.mp4
    │       S11E0866.mp4
    │       S11E0867.mp4
    │       S11E0868.mp4
    │       S11E0869.mp4
    │       S11E0870.mp4
    │       S11E0871.mp4
    │       S11E0872.mp4
    │       S11E0873.mp4
    │       S11E0874.mp4
    │       S11E0875.mp4
    │       S11E0876.mp4
    │       S11E0877.mp4
    │       S11E0878.mp4
    │
    ├───Season 12
    │       S12E0879.mp4
    │       S12E0880.mp4
    │       S12E0881.mp4
    │       S12E0882.mp4
    │       S12E0883.mp4
    │       S12E0884.mp4
    │       S12E0885.mp4
    │       S12E0886.mp4
    │       S12E0887.mp4
    │       S12E0888.mp4
    │       S12E0889.mp4
    │       S12E0890.mp4
    │       S12E0891.mp4
    │
    ├───Season 13
    │       S13E-SP - Special Episode Barto's Secret Room.mp4
    │       S13E-SP10 - Luffy-senpai Support Project! Barto's Secret Room 4!.mp4
    │       S13E-SP11 - A Very Special Feature! Momonosuke's Road to Becoming a Great Shogun.mp4
    │       S13E-SP2 - A Special Episode to Admire Zoro-senpai and Sanji-senpai! Barto's Secret Room 2!.mp4
    │       S13E-SP3 - A Comprehensive Anatomy! The Legend of Kozuki Oden!.mp4
    │       S13E-SP4 - The Captain’s Log of the Legend! Red-Haired Shanks!.mp4
    │       S13E-SP5 - A Comprehensive Anatomy! Fierce Fight! The Five from the New Generation.mp4
    │       S13E-SP6 - Recapping Fierce Fights! Straw Hats vs. Tobi Roppo.mp4
    │       S13E-SP7 - Recapping Fierce Fights! Zoro vs. A Lead Performer!.mp4
    │       S13E-SP8 - Recapping Fierce Fights! The Countercharge Alliance vs. Big Mom.mp4
    │       S13E-SP9 - Luffy-senpai Support Project! Barto's Secret Room 3!.mp4
    │       S13E0892.mp4
    │       S13E0893.mp4
    │       S13E0894.mp4
    │       S13E0895.mp4
    │       S13E0896.mp4
    │       S13E0897.mp4
    │       S13E0898.mp4
    │       S13E0899.mp4
    │       S13E0900.mp4
    │       S13E0901.mp4
    │       S13E0902.mp4
    │       S13E0903.mp4
    │       S13E0904.mp4
    │       S13E0905.mp4
    │       S13E0906.mp4
    │       S13E0907.mp4
    │       S13E0908.mp4
    │       S13E0909.mp4
    │       S13E0910.mp4
    │       S13E0911.mp4
    │       S13E0912.mp4
    │       S13E0913.mp4
    │       S13E0914.mp4
    │       S13E0915.mp4
    │       S13E0916.mp4
    │       S13E0917.mp4
    │       S13E0918.mp4
    │       S13E0919.mp4
    │       S13E0920.mp4
    │       S13E0921.mp4
    │       S13E0922.mp4
    │       S13E0923.mp4
    │       S13E0924.mp4
    │       S13E0925.mp4
    │       S13E0926.mp4
    │       S13E0927.mp4
    │       S13E0928.mp4
    │       S13E0929.mp4
    │       S13E0930.mp4
    │       S13E0931.mp4
    │       S13E0932.mp4
    │       S13E0933.mp4
    │       S13E0934.mp4
    │       S13E0935.mp4
    │       S13E0936.mp4
    │       S13E0937.mp4
    │       S13E0938.mp4
    │       S13E0939.mp4
    │       S13E0940.mp4
    │       S13E0941.mp4
    │       S13E0942.mp4
    │       S13E0943.mp4
    │       S13E0944.mp4
    │       S13E0945.mp4
    │       S13E0946.mp4
    │       S13E0947.mp4
    │       S13E0948.mp4
    │       S13E0949.mp4
    │       S13E0950.mp4
    │       S13E0951.mp4
    │       S13E0952.mp4
    │       S13E0953.mp4
    │       S13E0954.mp4
    │       S13E0955.mp4
    │       S13E0956.mp4
    │       S13E0957.mp4
    │       S13E0958.mp4
    │       S13E0959.mp4
    │       S13E0960.mp4
    │       S13E0961.mp4
    │       S13E0962.mp4
    │       S13E0963.mp4
    │       S13E0964.mp4
    │       S13E0965.mp4
    │       S13E0966.mp4
    │       S13E0967.mp4
    │       S13E0968.mp4
    │       S13E0969.mp4
    │       S13E0970.mp4
    │       S13E0971.mp4
    │       S13E0972.mp4
    │       S13E0973.mp4
    │       S13E0974.mp4
    │       S13E0975.mp4
    │       S13E0976.mp4
    │       S13E0977.mp4
    │       S13E0978.mp4
    │       S13E0979.mp4
    │       S13E0980.mp4
    │       S13E0981.mp4
    │       S13E0982.mp4
    │       S13E0983.mp4
    │       S13E0984.mp4
    │       S13E0985.mp4
    │       S13E0986.mp4
    │       S13E0987.mp4
    │       S13E0988.mp4
    │       S13E0989.mp4
    │       S13E0990.mp4
    │       S13E0991.mp4
    │       S13E0992.mp4
    │       S13E0993.mp4
    │       S13E0994.mp4
    │       S13E0995.mp4
    │       S13E0996.mp4
    │       S13E0997.mp4
    │       S13E0998.mp4
    │       S13E0999.mp4
    │       S13E1000.mp4
    │       S13E1001.mp4
    │       S13E1002.mp4
    │       S13E1003.mp4
    │       S13E1004.mp4
    │       S13E1005.mp4
    │       S13E1006.mp4
    │       S13E1007.mp4
    │       S13E1008.mp4
    │       S13E1009.mp4
    │       S13E1010.mp4
    │       S13E1011.mp4
    │       S13E1012.mp4
    │       S13E1013.mp4
    │       S13E1014.mp4
    │       S13E1015.mp4
    │       S13E1016.mp4
    │       S13E1017.mp4
    │       S13E1018.mp4
    │       S13E1019.mp4
    │       S13E1020.mp4
    │       S13E1021.mp4
    │       S13E1022.mp4
    │       S13E1023.mp4
    │       S13E1024.mp4
    │       S13E1025.mp4
    │       S13E1026.mp4
    │       S13E1027.mp4
    │       S13E1028.mp4
    │       S13E1029.mp4
    │       S13E1030.mp4
    │       S13E1031.mp4
    │       S13E1032.mp4
    │       S13E1033.mp4
    │       S13E1034.mp4
    │       S13E1035.mp4
    │       S13E1036.mp4
    │       S13E1037.mp4
    │       S13E1038.mp4
    │       S13E1039.mp4
    │       S13E1040.mp4
    │       S13E1041.mp4
    │       S13E1042.mp4
    │       S13E1043.mp4
    │       S13E1044.mp4
    │       S13E1045.mp4
    │       S13E1046.mp4
    │       S13E1047.mp4
    │       S13E1048.mp4
    │       S13E1049.mp4
    │       S13E1050.mp4
    │       S13E1051.mp4
    │       S13E1052.mp4
    │       S13E1053.mp4
    │       S13E1054.mp4
    │       S13E1055.mp4
    │       S13E1056.mp4
    │       S13E1057.mp4
    │       S13E1058.mp4
    │       S13E1059.mp4
    │       S13E1060.mp4
    │       S13E1061.mp4
    │       S13E1062.mp4
    │       S13E1063.mp4
    │       S13E1064.mp4
    │       S13E1065.mp4
    │       S13E1066.mp4
    │       S13E1067.mp4
    │       S13E1068.mp4
    │       S13E1069.mp4
    │       S13E1070.mp4
    │       S13E1071.mp4
    │       S13E1072.mp4
    │       S13E1073.mp4
    │       S13E1074.mp4
    │       S13E1075.mp4
    │       S13E1076.mp4
    │       S13E1077.mp4
    │       S13E1078.mp4
    │       S13E1079.mp4
    │       S13E1080.mp4
    │       S13E1081.mp4
    │       S13E1082.mp4
    │       S13E1083.mp4
    │       S13E1084.mp4
    │       S13E1085.mp4
    │       S13E1086.mp4
    │       S13E1087.mp4
    │       S13E1088.mp4
    │
    ├───Season 14
    │       S14E-SP12 - A Project to Fully Enjoy! ‘Surgeon of Death’ Trafalgar Law.mp4
    │       S14E-SP13 - The Log of the Rivalry! The Straw Hats vs. Cipher Pol.mp4
    │       S14E-SP14 - Making History! The Turbulent Old and New Four Emperors!.mp4
    │       S14E-SP15 - The Log of the Turbulent Revolution! The Revolutionary Army Maneuvers in Secret!.mp4
    │       S14E-SP16 - Unwavering Justice! The Navy's Proud Log!.mp4
    │       S14E1089.mp4
    │       S14E1090.mp4
    │       S14E1091.mp4
    │       S14E1092.mp4
    │       S14E1093.mp4
    │       S14E1094.mp4
    │       S14E1095.mp4
    │       S14E1096.mp4
    │       S14E1097.mp4
    │       S14E1098.mp4
    │       S14E1099.mp4
    │       S14E1100.mp4
    │       S14E1101.mp4
    │       S14E1102.mp4
    │       S14E1103.mp4
    │       S14E1104.mp4
    │       S14E1105.mp4
    │       S14E1106.mp4
    │       S14E1107.mp4
    │       S14E1108.mp4
    │       S14E1109.mp4
    │       S14E1110.mp4
    │       S14E1111.mp4
    │       S14E1112.mp4
    │       S14E1113.mp4
    │       S14E1114.mp4
    │       S14E1115.mp4
    │       S14E1116.mp4
    │       S14E1117.mp4
    │       S14E1118.mp4
    │       S14E1119.mp4
    │       S14E1120.mp4
    │       S14E1121.mp4
    │       S14E1122.mp4
    │       S14E1123.mp4
    │       S14E1124.mp4
    │       S14E1125.mp4
    │       S14E1126.mp4
    │       S14E1127.mp4
    │
    ├───Season 2
    │       S2E0062.mp4
    │       S2E0063.mp4
    │       S2E0064.mp4
    │       S2E0065.mp4
    │       S2E0066.mp4
    │       S2E0067.mp4
    │       S2E0068.mp4
    │       S2E0069.mp4
    │       S2E0070.mp4
    │       S2E0071.mp4
    │       S2E0072.mp4
    │       S2E0073.mp4
    │       S2E0074.mp4
    │       S2E0075.mp4
    │       S2E0076.mp4
    │       S2E0077.mp4
    │       S2E0078.mp4
    │       S2E0079.mp4
    │       S2E0080.mp4
    │       S2E0081.mp4
    │       S2E0082.mp4
    │       S2E0083.mp4
    │       S2E0084.mp4
    │       S2E0085.mp4
    │       S2E0086.mp4
    │       S2E0087.mp4
    │       S2E0088.mp4
    │       S2E0089.mp4
    │       S2E0090.mp4
    │       S2E0091.mp4
    │       S2E0092.mp4
    │       S2E0093.mp4
    │       S2E0094.mp4
    │       S2E0095.mp4
    │       S2E0096.mp4
    │       S2E0097.mp4
    │       S2E0098.mp4
    │       S2E0099.mp4
    │       S2E0100.mp4
    │       S2E0101.mp4
    │       S2E0102.mp4
    │       S2E0103.mp4
    │       S2E0104.mp4
    │       S2E0105.mp4
    │       S2E0106.mp4
    │       S2E0107.mp4
    │       S2E0108.mp4
    │       S2E0109.mp4
    │       S2E0110.mp4
    │       S2E0111.mp4
    │       S2E0112.mp4
    │       S2E0113.mp4
    │       S2E0114.mp4
    │       S2E0115.mp4
    │       S2E0116.mp4
    │       S2E0117.mp4
    │       S2E0118.mp4
    │       S2E0119.mp4
    │       S2E0120.mp4
    │       S2E0121.mp4
    │       S2E0122.mp4
    │       S2E0123.mp4
    │       S2E0124.mp4
    │       S2E0125.mp4
    │       S2E0126.mp4
    │       S2E0127.mp4
    │       S2E0128.mp4
    │       S2E0129.mp4
    │       S2E0130.mp4
    │       S2E0131.mp4
    │       S2E0132.mp4
    │       S2E0133.mp4
    │       S2E0134.mp4
    │       S2E0135.mp4
    │
    ├───Season 3
    │       S3E0136.mp4
    │       S3E0137.mp4
    │       S3E0138.mp4
    │       S3E0139.mp4
    │       S3E0140.mp4
    │       S3E0141.mp4
    │       S3E0142.mp4
    │       S3E0143.mp4
    │       S3E0144.mp4
    │       S3E0145.mp4
    │       S3E0146.mp4
    │       S3E0147.mp4
    │       S3E0148.mp4
    │       S3E0149.mp4
    │       S3E0150.mp4
    │       S3E0151.mp4
    │       S3E0152.mp4
    │       S3E0153.mp4
    │       S3E0154.mp4
    │       S3E0155.mp4
    │       S3E0156.mp4
    │       S3E0157.mp4
    │       S3E0158.mp4
    │       S3E0159.mp4
    │       S3E0160.mp4
    │       S3E0161.mp4
    │       S3E0162.mp4
    │       S3E0163.mp4
    │       S3E0164.mp4
    │       S3E0165.mp4
    │       S3E0166.mp4
    │       S3E0167.mp4
    │       S3E0168.mp4
    │       S3E0169.mp4
    │       S3E0170.mp4
    │       S3E0171.mp4
    │       S3E0172.mp4
    │       S3E0173.mp4
    │       S3E0174.mp4
    │       S3E0175.mp4
    │       S3E0176.mp4
    │       S3E0177.mp4
    │       S3E0178.mp4
    │       S3E0179.mp4
    │       S3E0180.mp4
    │       S3E0181.mp4
    │       S3E0182.mp4
    │       S3E0183.mp4
    │       S3E0184.mp4
    │       S3E0185.mp4
    │       S3E0186.mp4
    │       S3E0187.mp4
    │       S3E0188.mp4
    │       S3E0189.mp4
    │       S3E0190.mp4
    │       S3E0191.mp4
    │       S3E0192.mp4
    │       S3E0193.mp4
    │       S3E0194.mp4
    │       S3E0195.mp4
    │       S3E0196.mp4
    │       S3E0197.mp4
    │       S3E0198.mp4
    │       S3E0199.mp4
    │       S3E0200.mp4
    │       S3E0201.mp4
    │       S3E0202.mp4
    │       S3E0203.mp4
    │       S3E0204.mp4
    │       S3E0205.mp4
    │       S3E0206.mp4
    │
    ├───Season 4
    │       S4E0207.mp4
    │       S4E0208.mp4
    │       S4E0209.mp4
    │       S4E0210.mp4
    │       S4E0211.mp4
    │       S4E0212.mp4
    │       S4E0213.mp4
    │       S4E0214.mp4
    │       S4E0215.mp4
    │       S4E0216.mp4
    │       S4E0217.mp4
    │       S4E0218.mp4
    │       S4E0219.mp4
    │       S4E0220.mp4
    │       S4E0221.mp4
    │       S4E0222.mp4
    │       S4E0223.mp4
    │       S4E0224.mp4
    │       S4E0225.mp4
    │       S4E0226.mp4
    │       S4E0227.mp4
    │       S4E0228.mp4
    │       S4E0229.mp4
    │       S4E0230.mp4
    │       S4E0231.mp4
    │       S4E0232.mp4
    │       S4E0233.mp4
    │       S4E0234.mp4
    │       S4E0235.mp4
    │       S4E0236.mp4
    │       S4E0237.mp4
    │       S4E0238.mp4
    │       S4E0239.mp4
    │       S4E0240.mp4
    │       S4E0241.mp4
    │       S4E0242.mp4
    │       S4E0243.mp4
    │       S4E0244.mp4
    │       S4E0245.mp4
    │       S4E0246.mp4
    │       S4E0247.mp4
    │       S4E0248.mp4
    │       S4E0249.mp4
    │       S4E0250.mp4
    │       S4E0251.mp4
    │       S4E0252.mp4
    │       S4E0253.mp4
    │       S4E0254.mp4
    │       S4E0255.mp4
    │       S4E0256.mp4
    │       S4E0257.mp4
    │       S4E0258.mp4
    │       S4E0259.mp4
    │       S4E0260.mp4
    │       S4E0261.mp4
    │       S4E0262.mp4
    │       S4E0263.mp4
    │       S4E0264.mp4
    │       S4E0265.mp4
    │       S4E0266.mp4
    │       S4E0267.mp4
    │       S4E0268.mp4
    │       S4E0269.mp4
    │       S4E0270.mp4
    │       S4E0271.mp4
    │       S4E0272.mp4
    │       S4E0273.mp4
    │       S4E0274.mp4
    │       S4E0275.mp4
    │       S4E0276.mp4
    │       S4E0277.mp4
    │       S4E0278.mp4
    │       S4E0279.mp4
    │       S4E0280.mp4
    │       S4E0281.mp4
    │       S4E0282.mp4
    │       S4E0283.mp4
    │       S4E0284.mp4
    │       S4E0285.mp4
    │       S4E0286.mp4
    │       S4E0287.mp4
    │       S4E0288.mp4
    │       S4E0289.mp4
    │       S4E0290.mp4
    │       S4E0291.mp4
    │       S4E0292.mp4
    │       S4E0293.mp4
    │       S4E0294.mp4
    │       S4E0295.mp4
    │       S4E0296.mp4
    │       S4E0297.mp4
    │       S4E0298.mp4
    │       S4E0299.mp4
    │       S4E0300.mp4
    │       S4E0301.mp4
    │       S4E0302.mp4
    │       S4E0303.mp4
    │       S4E0304.mp4
    │       S4E0305.mp4
    │       S4E0306.mp4
    │       S4E0307.mp4
    │       S4E0308.mp4
    │       S4E0309.mp4
    │       S4E0310.mp4
    │       S4E0311.mp4
    │       S4E0312.mp4
    │       S4E0313.mp4
    │       S4E0314.mp4
    │       S4E0315.mp4
    │       S4E0316.mp4
    │       S4E0317.mp4
    │       S4E0318.mp4
    │       S4E0319.mp4
    │       S4E0320.mp4
    │       S4E0321.mp4
    │       S4E0322.mp4
    │       S4E0323.mp4
    │       S4E0324.mp4
    │       S4E0325.mp4
    │
    ├───Season 5
    │       S5E0326.mp4
    │       S5E0327.mp4
    │       S5E0328.mp4
    │       S5E0329.mp4
    │       S5E0330.mp4
    │       S5E0331.mp4
    │       S5E0332.mp4
    │       S5E0333.mp4
    │       S5E0334.mp4
    │       S5E0335.mp4
    │       S5E0336.mp4
    │       S5E0337.mp4
    │       S5E0338.mp4
    │       S5E0339.mp4
    │       S5E0340.mp4
    │       S5E0341.mp4
    │       S5E0342.mp4
    │       S5E0343.mp4
    │       S5E0344.mp4
    │       S5E0345.mp4
    │       S5E0346.mp4
    │       S5E0347.mp4
    │       S5E0348.mp4
    │       S5E0349.mp4
    │       S5E0350.mp4
    │       S5E0351.mp4
    │       S5E0352.mp4
    │       S5E0353.mp4
    │       S5E0354.mp4
    │       S5E0355.mp4
    │       S5E0356.mp4
    │       S5E0357.mp4
    │       S5E0358.mp4
    │       S5E0359.mp4
    │       S5E0360.mp4
    │       S5E0361.mp4
    │       S5E0362.mp4
    │       S5E0363.mp4
    │       S5E0364.mp4
    │       S5E0365.mp4
    │       S5E0366.mp4
    │       S5E0367.mp4
    │       S5E0368.mp4
    │       S5E0369.mp4
    │       S5E0370.mp4
    │       S5E0371.mp4
    │       S5E0372.mp4
    │       S5E0373.mp4
    │       S5E0374.mp4
    │       S5E0375.mp4
    │       S5E0376.mp4
    │       S5E0377.mp4
    │       S5E0378.mp4
    │       S5E0379.mp4
    │       S5E0380.mp4
    │       S5E0381.mp4
    │       S5E0382.mp4
    │       S5E0383.mp4
    │       S5E0384.mp4
    │
    ├───Season 6
    │       S6E0385.mp4
    │       S6E0386.mp4
    │       S6E0387.mp4
    │       S6E0388.mp4
    │       S6E0389.mp4
    │       S6E0390.mp4
    │       S6E0391.mp4
    │       S6E0392.mp4
    │       S6E0393.mp4
    │       S6E0394.mp4
    │       S6E0395.mp4
    │       S6E0396.mp4
    │       S6E0397.mp4
    │       S6E0398.mp4
    │       S6E0399.mp4
    │       S6E0400.mp4
    │       S6E0401.mp4
    │       S6E0402.mp4
    │       S6E0403.mp4
    │       S6E0404.mp4
    │       S6E0405.mp4
    │       S6E0406.mp4
    │       S6E0407.mp4
    │       S6E0408.mp4
    │       S6E0409.mp4
    │       S6E0410.mp4
    │       S6E0411.mp4
    │       S6E0412.mp4
    │       S6E0413.mp4
    │       S6E0414.mp4
    │       S6E0415.mp4
    │       S6E0416.mp4
    │       S6E0417.mp4
    │       S6E0418.mp4
    │       S6E0419.mp4
    │       S6E0420.mp4
    │       S6E0421.mp4
    │       S6E0422.mp4
    │       S6E0423.mp4
    │       S6E0424.mp4
    │       S6E0425.mp4
    │       S6E0426.mp4
    │       S6E0427.mp4
    │       S6E0428.mp4
    │       S6E0429.mp4
    │       S6E0430.mp4
    │       S6E0431.mp4
    │       S6E0432.mp4
    │       S6E0433.mp4
    │       S6E0434.mp4
    │       S6E0435.mp4
    │       S6E0436.mp4
    │       S6E0437.mp4
    │       S6E0438.mp4
    │       S6E0439.mp4
    │       S6E0440.mp4
    │       S6E0441.mp4
    │       S6E0442.mp4
    │       S6E0443.mp4
    │       S6E0444.mp4
    │       S6E0445.mp4
    │       S6E0446.mp4
    │       S6E0447.mp4
    │       S6E0448.mp4
    │       S6E0449.mp4
    │       S6E0450.mp4
    │       S6E0451.mp4
    │       S6E0452.mp4
    │       S6E0453.mp4
    │       S6E0454.mp4
    │       S6E0455.mp4
    │       S6E0456.mp4
    │       S6E0457.mp4
    │       S6E0458.mp4
    │       S6E0459.mp4
    │       S6E0460.mp4
    │       S6E0461.mp4
    │       S6E0462.mp4
    │       S6E0463.mp4
    │       S6E0464.mp4
    │       S6E0465.mp4
    │       S6E0466.mp4
    │       S6E0467.mp4
    │       S6E0468.mp4
    │       S6E0469.mp4
    │       S6E0470.mp4
    │       S6E0471.mp4
    │       S6E0472.mp4
    │       S6E0473.mp4
    │       S6E0474.mp4
    │       S6E0475.mp4
    │       S6E0476.mp4
    │       S6E0477.mp4
    │       S6E0478.mp4
    │       S6E0479.mp4
    │       S6E0480.mp4
    │       S6E0481.mp4
    │       S6E0482.mp4
    │       S6E0483.mp4
    │       S6E0484.mp4
    │       S6E0485.mp4
    │       S6E0486.mp4
    │       S6E0487.mp4
    │       S6E0488.mp4
    │       S6E0489.mp4
    │       S6E0490.mp4
    │       S6E0491.mp4
    │       S6E0492.mp4
    │       S6E0493.mp4
    │       S6E0494.mp4
    │       S6E0495.mp4
    │       S6E0496.mp4
    │       S6E0497.mp4
    │       S6E0498.mp4
    │       S6E0499.mp4
    │       S6E0500.mp4
    │       S6E0501.mp4
    │       S6E0502.mp4
    │       S6E0503.mp4
    │       S6E0504.mp4
    │       S6E0505.mp4
    │       S6E0506.mp4
    │       S6E0507.mp4
    │       S6E0508.mp4
    │       S6E0509.mp4
    │       S6E0510.mp4
    │       S6E0511.mp4
    │       S6E0512.mp4
    │       S6E0513.mp4
    │       S6E0514.mp4
    │       S6E0515.mp4
    │
    ├───Season 7
    │       S7E0517.mp4
    │       S7E0518.mp4
    │       S7E0519.mp4
    │       S7E0520.mp4
    │       S7E0521.mp4
    │       S7E0522.mp4
    │       S7E0523.mp4
    │       S7E0524.mp4
    │       S7E0525.mp4
    │       S7E0526.mp4
    │       S7E0527.mp4
    │       S7E0528.mp4
    │       S7E0529.mp4
    │       S7E0530.mp4
    │       S7E0531.mp4
    │       S7E0532.mp4
    │       S7E0533.mp4
    │       S7E0534.mp4
    │       S7E0535.mp4
    │       S7E0536.mp4
    │       S7E0537.mp4
    │       S7E0538.mp4
    │       S7E0539.mp4
    │       S7E0540.mp4
    │       S7E0541.mp4
    │       S7E0542.mp4
    │       S7E0543.mp4
    │       S7E0544.mp4
    │       S7E0545.mp4
    │       S7E0546.mp4
    │       S7E0547.mp4
    │       S7E0548.mp4
    │       S7E0549.mp4
    │       S7E0550.mp4
    │       S7E0551.mp4
    │       S7E0552.mp4
    │       S7E0553.mp4
    │       S7E0554.mp4
    │       S7E0555.mp4
    │       S7E0556.mp4
    │       S7E0557.mp4
    │       S7E0558.mp4
    │       S7E0559.mp4
    │       S7E0560.mp4
    │       S7E0561.mp4
    │       S7E0562.mp4
    │       S7E0563.mp4
    │       S7E0564.mp4
    │       S7E0565.mp4
    │       S7E0566.mp4
    │       S7E0567.mp4
    │       S7E0568.mp4
    │       S7E0569.mp4
    │       S7E0570.mp4
    │       S7E0571.mp4
    │       S7E0572.mp4
    │       S7E0573.mp4
    │
    ├───Season 8
    │       S8E0575.mp4
    │       S8E0576.mp4
    │       S8E0577.mp4
    │       S8E0578.mp4
    │       S8E0579.mp4
    │       S8E0580.mp4
    │       S8E0581.mp4
    │       S8E0582.mp4
    │       S8E0583.mp4
    │       S8E0584.mp4
    │       S8E0585.mp4
    │       S8E0586.mp4
    │       S8E0587.mp4
    │       S8E0588.mp4
    │       S8E0589.mp4
    │       S8E0590.mp4
    │       S8E0591.mp4
    │       S8E0592.mp4
    │       S8E0593.mp4
    │       S8E0594.mp4
    │       S8E0595.mp4
    │       S8E0596.mp4
    │       S8E0597.mp4
    │       S8E0598.mp4
    │       S8E0599.mp4
    │       S8E0600.mp4
    │       S8E0601.mp4
    │       S8E0602.mp4
    │       S8E0603.mp4
    │       S8E0604.mp4
    │       S8E0605.mp4
    │       S8E0606.mp4
    │       S8E0607.mp4
    │       S8E0608.mp4
    │       S8E0609.mp4
    │       S8E0610.mp4
    │       S8E0611.mp4
    │       S8E0612.mp4
    │       S8E0613.mp4
    │       S8E0614.mp4
    │       S8E0615.mp4
    │       S8E0616.mp4
    │       S8E0617.mp4
    │       S8E0618.mp4
    │       S8E0619.mp4
    │       S8E0620.mp4
    │       S8E0621.mp4
    │       S8E0622.mp4
    │       S8E0623.mp4
    │       S8E0624.mp4
    │       S8E0625.mp4
    │       S8E0626.mp4
    │       S8E0627.mp4
    │       S8E0628.mp4
    │
    ├───Season 9
    │       S9E0630.mp4
    │       S9E0631.mp4
    │       S9E0632.mp4
    │       S9E0633.mp4
    │       S9E0634.mp4
    │       S9E0635.mp4
    │       S9E0636.mp4
    │       S9E0637.mp4
    │       S9E0638.mp4
    │       S9E0639.mp4
    │       S9E0640.mp4
    │       S9E0641.mp4
    │       S9E0642.mp4
    │       S9E0643.mp4
    │       S9E0644.mp4
    │       S9E0645.mp4
    │       S9E0646.mp4
    │       S9E0647.mp4
    │       S9E0648.mp4
    │       S9E0649.mp4
    │       S9E0650.mp4
    │       S9E0651.mp4
    │       S9E0652.mp4
    │       S9E0653.mp4
    │       S9E0654.mp4
    │       S9E0655.mp4
    │       S9E0656.mp4
    │       S9E0657.mp4
    │       S9E0658.mp4
    │       S9E0659.mp4
    │       S9E0660.mp4
    │       S9E0661.mp4
    │       S9E0662.mp4
    │       S9E0663.mp4
    │       S9E0664.mp4
    │       S9E0665.mp4
    │       S9E0666.mp4
    │       S9E0667.mp4
    │       S9E0668.mp4
    │       S9E0669.mp4
    │       S9E0670.mp4
    │       S9E0671.mp4
    │       S9E0672.mp4
    │       S9E0673.mp4
    │       S9E0674.mp4
    │       S9E0675.mp4
    │       S9E0676.mp4
    │       S9E0677.mp4
    │       S9E0678.mp4
    │       S9E0679.mp4
    │       S9E0680.mp4
    │       S9E0681.mp4
    │       S9E0682.mp4
    │       S9E0683.mp4
    │       S9E0684.mp4
    │       S9E0685.mp4
    │       S9E0686.mp4
    │       S9E0687.mp4
    │       S9E0688.mp4
    │       S9E0689.mp4
    │       S9E0690.mp4
    │       S9E0691.mp4
    │       S9E0692.mp4
    │       S9E0693.mp4
    │       S9E0694.mp4
    │       S9E0695.mp4
    │       S9E0696.mp4
    │       S9E0697.mp4
    │       S9E0698.mp4
    │       S9E0699.mp4
    │
    └───Season 9 - 2
            S9E0700.mp4
            S9E0701.mp4
            S9E0702.mp4
            S9E0703.mp4
            S9E0704.mp4
            S9E0705.mp4
            S9E0706.mp4
            S9E0707.mp4
            S9E0708.mp4
            S9E0709.mp4
            S9E0710.mp4
            S9E0711.mp4
            S9E0712.mp4
            S9E0713.mp4
            S9E0714.mp4
            S9E0715.mp4
            S9E0716.mp4
            S9E0717.mp4
            S9E0718.mp4
            S9E0719.mp4
            S9E0720.mp4
            S9E0721.mp4
            S9E0722.mp4
            S9E0723.mp4
            S9E0724.mp4
            S9E0725.mp4
            S9E0726.mp4
            S9E0727.mp4
            S9E0728.mp4
            S9E0729.mp4
            S9E0730.mp4
            S9E0731.mp4
            S9E0732.mp4
            S9E0733.mp4
            S9E0734.mp4
            S9E0735.mp4
            S9E0736.mp4
            S9E0737.mp4
            S9E0738.mp4
            S9E0739.mp4
            S9E0740.mp4
            S9E0741.mp4
            S9E0742.mp4
            S9E0743.mp4
            S9E0744.mp4
            S9E0745.mp4
            S9E0746.mp4
```

</details>

<details>
  <summary>Sword Art Online</summary>

```
├───Alicization War of Underworld
│       E01.mp4
│       E02.mp4
│       E03.mp4
│       E04.mp4
│       E05.mp4
│       E06.mp4
│       E07.mp4
│       E08.mp4
│       E09.mp4
│       E10.mp4
│       E11.mp4
│       E12.mp4
│       E13.mp4
│       E14.mp4
│       E15.mp4
│       E16.mp4
│       E17.mp4
│       E18.mp4
│       E19.mp4
│       E20.mp4
│       E21.mp4
│       E22.mp4
│       E23.mp4
│
├───Season 1
│       S1E0001.mp4
│       S1E0002.mp4
│       S1E0003.mp4
│       S1E0004.mp4
│       S1E0005.mp4
│       S1E0006.mp4
│       S1E0007.mp4
│       S1E0008.mp4
│       S1E0009.mp4
│       S1E0010.mp4
│       S1E0011.mp4
│       S1E0012.mp4
│       S1E0013.mp4
│       S1E0014.mp4
│       S1E0015.mp4
│       S1E0016.mp4
│       S1E0017.mp4
│       S1E0018.mp4
│       S1E0019.mp4
│       S1E0020.mp4
│       S1E0021.mp4
│       S1E0022.mp4
│       S1E0023.mp4
│       S1E0024.mp4
│       S1E0025.mp4
│
├───Season 2
│       S2E0001.mp4
│       S2E0002.mp4
│       S2E0003.mp4
│       S2E0004.mp4
│       S2E0005.mp4
│       S2E0006.mp4
│       S2E0007.mp4
│       S2E0008.mp4
│       S2E0009.mp4
│       S2E0010.mp4
│       S2E0011.mp4
│       S2E0012.mp4
│       S2E0013.mp4
│       S2E0014.mp4
│       S2E0015.mp4
│       S2E0016.mp4
│       S2E0017.mp4
│       S2E0018.mp4
│       S2E0019.mp4
│       S2E0020.mp4
│       S2E0021.mp4
│       S2E0022.mp4
│       S2E0023.mp4
│       S2E0024.mp4
│       S2E14.5.mp4
│
├───Season 3
│       S3E0001.mp4
│       S3E0002.mp4
│       S3E0003.mp4
│       S3E0004.mp4
│       S3E0005.mp4
│       S3E0006.mp4
│       S3E0007.mp4
│       S3E0008.mp4
│       S3E0009.mp4
│       S3E0010.mp4
│       S3E0011.mp4
│       S3E0012.mp4
│       S3E0013.mp4
│       S3E0014.mp4
│       S3E0015.mp4
│       S3E0016.mp4
│       S3E0017.mp4
│       S3E0018.mp4
│       S3E0019.mp4
│       S3E0020.mp4
│       S3E0021.mp4
│       S3E0022.mp4
│       S3E0023.mp4
│       S3E0024.mp4
│
├───Sword Art Online the Movie -Ordinal Scale-
│       Ordinal Scale.mp4
│
├───Sword Art Online the Movie -Progressive- Aria of a Starless Night
│       Sword Art Online the Movie -Progressive- Aria of a Starless Night.mp4
│
└───Sword Art Online the Movie -Progressive- Scherzo of Deep Night
        Sword Art Online the Movie -Progressive- Scherzo.mp4
```

</details>

<details>
  <summary>Blue Exorcist</summary>

```
├───Season 3 - (1) Shimane Illuminati Saga
│       S03E01.mp4
│       S03E02.mp4
│       S03E03.mp4
│       S03E04.mp4
│       S03E05.mp4
│       S03E06.mp4
│       S03E07.mp4
│       S03E08.mp4
│       S03E09.mp4
│       S03E10.mp4
│       S03E11.mp4
│       S03E12.mp4
│
└───Season 3 - (2) Beyond the Snow Saga
        S03E01.mp4
        S03E02.mp4
        S03E03.mp4
        S03E04.mp4
        S03E05.mp4
        S03E06.mp4
```

</details>

<details>
  <summary>CARDFIGHT!! VANGUARD overDress</summary>

```
├───CARDFIGHT!! VANGUARD Divinez Season 2
│       E01.mp4
│       E02.mp4
│       E03.mp4
│       E04.mp4
│       E05.mp4
│       E06.mp4
│       E07.mp4
│       E08.mp4
│       E09.mp4
│       E10.mp4
│       E11.mp4
│       E12.mp4
│       E13.mp4
│
├───Season 1
│       S01E0001.mp4
│       S01E0002.mp4
│       S01E0003.mp4
│       S01E0004.mp4
│       S01E0005.mp4
│       S01E0006.mp4
│       S01E0007.mp4
│       S01E0008.mp4
│       S01E0009.mp4
│       S01E0010.mp4
│       S01E0011.mp4
│       S01E0012.mp4
│       S01E0013.mp4
│       S01E0014.mp4
│       S01E0015.mp4
│       S01E0016.mp4
│       S01E0017.mp4
│       S01E0018.mp4
│       S01E0019.mp4
│       S01E0020.mp4
│       S01E0021.mp4
│       S01E0022.mp4
│       S01E0023.mp4
│       S01E0024.mp4
│       S01E0025.mp4
│
├───Season 1 - CARDFIGHT!! VANGUARD will+Dress
│       E01.mp4
│       E02.mp4
│       E03.mp4
│       E04.mp4
│       E05.mp4
│       E06.mp4
│       E07.mp4
│       E08.mp4
│       E09.mp4
│       E10.mp4
│       E11.mp4
│       E12.mp4
│       E13.mp4
│
├───Season 2
│       S02E0001.mp4
│       S02E0002.mp4
│       S02E0003.mp4
│       S02E0004.mp4
│       S02E0005.mp4
│       S02E0006.mp4
│       S02E0007.mp4
│       S02E0008.mp4
│       S02E0009.mp4
│       S02E0010.mp4
│       S02E0011.mp4
│       S02E0012.mp4
│       S02E0013.mp4
│
├───Season 3
│       S03E0001.mp4
│       S03E0002.mp4
│       S03E0003.mp4
│       S03E0004.mp4
│       S03E0005.mp4
│       S03E0006.mp4
│       S03E0007.mp4
│       S03E0008.mp4
│       S03E0009.mp4
│       S03E0010.mp4
│       S03E0011.mp4
│       S03E0012.mp4
│       S03E0013.mp4
│
└───Season 4
        S04E0001.mp4
        S04E0002.mp4
        S04E0003.mp4
        S04E0004.mp4
        S04E0005.mp4
        S04E0006.mp4
        S04E0007.mp4
        S04E0008.mp4
        S04E0009.mp4
        S04E0010.mp4
        S04E0011.mp4
        S04E0012.mp4
        S04E0013.mp4
```

</details>

<details>
  <summary>JoJo's Bizarre Adventure</summary>

```
├───Season 1
│       S01E0001.mp4
│       S01E0002.mp4
│       S01E0003.mp4
│       S01E0004.mp4
│       S01E0005.mp4
│       S01E0006.mp4
│       S01E0007.mp4
│       S01E0008.mp4
│       S01E0009.mp4
│       S01E0010.mp4
│       S01E0011.mp4
│       S01E0012.mp4
│       S01E0013.mp4
│       S01E0014.mp4
│       S01E0015.mp4
│       S01E0016.mp4
│       S01E0017.mp4
│       S01E0018.mp4
│       S01E0019.mp4
│       S01E0020.mp4
│       S01E0021.mp4
│       S01E0022.mp4
│       S01E0023.mp4
│       S01E0024.mp4
│       S01E0025.mp4
│       S01E0026.mp4
│
├───Season 1 - Re-Edited
│       S01E01.mp4
│       S01E02.mp4
│       S01E03.mp4
│
├───Season 2
│       S02E0001.mp4
│       S02E0002.mp4
│       S02E0003.mp4
│       S02E0004.mp4
│       S02E0005.mp4
│       S02E0006.mp4
│       S02E0007.mp4
│       S02E0008.mp4
│       S02E0009.mp4
│       S02E0010.mp4
│       S02E0011.mp4
│       S02E0012.mp4
│       S02E0013.mp4
│       S02E0014.mp4
│       S02E0015.mp4
│       S02E0016.mp4
│       S02E0017.mp4
│       S02E0018.mp4
│       S02E0019.mp4
│       S02E0020.mp4
│       S02E0021.mp4
│       S02E0022.mp4
│       S02E0023.mp4
│       S02E0024.mp4
│
├───Season 2 - Battle in Egypt
│       S02E25.mp4
│       S02E26.mp4
│       S02E27.mp4
│       S02E28.mp4
│       S02E29.mp4
│       S02E30.mp4
│       S02E31.mp4
│       S02E32.mp4
│       S02E33.mp4
│       S02E34.mp4
│       S02E35.mp4
│       S02E36.mp4
│       S02E37.mp4
│       S02E38.mp4
│       S02E39.mp4
│       S02E40.mp4
│       S02E41.mp4
│       S02E42.mp4
│       S02E43.mp4
│       S02E44.mp4
│       S02E45.mp4
│       S02E46.mp4
│       S02E47.mp4
│       S02E48.mp4
│
├───Season 3
│       S03E0001.mp4
│       S03E0002.mp4
│       S03E0003.mp4
│       S03E0004.mp4
│       S03E0005.mp4
│       S03E0006.mp4
│       S03E0007.mp4
│       S03E0008.mp4
│       S03E0009.mp4
│       S03E0010.mp4
│       S03E0011.mp4
│       S03E0012.mp4
│       S03E0013.mp4
│       S03E0014.mp4
│       S03E0015.mp4
│       S03E0016.mp4
│       S03E0017.mp4
│       S03E0018.mp4
│       S03E0019.mp4
│       S03E0020.mp4
│       S03E0021.mp4
│       S03E0022.mp4
│       S03E0023.mp4
│       S03E0024.mp4
│       S03E0025.mp4
│       S03E0026.mp4
│       S03E0027.mp4
│       S03E0028.mp4
│       S03E0029.mp4
│       S03E0030.mp4
│       S03E0031.mp4
│       S03E0032.mp4
│       S03E0033.mp4
│       S03E0034.mp4
│       S03E0035.mp4
│       S03E0036.mp4
│       S03E0037.mp4
│       S03E0038.mp4
│       S03E0039.mp4
│
└───Season 4
        S04E0001.mp4
        S04E0002.mp4
        S04E0003.mp4
        S04E0004.mp4
        S04E0005.mp4
        S04E0006.mp4
        S04E0007.mp4
        S04E0008.mp4
        S04E0009.mp4
        S04E0010.mp4
        S04E0011.mp4
        S04E0012.mp4
        S04E0013.5.mp4
        S04E0013.mp4
        S04E0014.mp4
        S04E0015.mp4
        S04E0016.mp4
        S04E0017.mp4
        S04E0018.mp4
        S04E0019.mp4
        S04E0020.mp4
        S04E0021.5.mp4
        S04E0021.mp4
        S04E0022.mp4
        S04E0023.mp4
        S04E0024.mp4
        S04E0025.mp4
        S04E0026.mp4
        S04E0027.mp4
        S04E0028.5.mp4
        S04E0028.mp4
        S04E0029.mp4
        S04E0030.mp4
        S04E0031.mp4
        S04E0032.mp4
        S04E0033.mp4
        S04E0034.mp4
        S04E0035.mp4
        S04E0036.mp4
        S04E0037.mp4
        S04E0038.mp4
        S04E0039.mp4
```

</details>

<details>
  <summary>Laid-Back Camp</summary>

```
├───Laid-Back Camp Movie
│       Laid-Back Camp Movie.mp4
│
├───Season 1
│       S01E0001.mp4
│       S01E0002.mp4
│       S01E0003.mp4
│       S01E0004.mp4
│       S01E0005.mp4
│       S01E0006.mp4
│       S01E0007.mp4
│       S01E0008.mp4
│       S01E0009.mp4
│       S01E0010.mp4
│       S01E0011.mp4
│       S01E0012.mp4
│
├───Season 2
│       S02E0001.mp4
│       S02E0002.mp4
│       S02E0003.mp4
│       S02E0004.mp4
│       S02E0005.mp4
│       S02E0006.mp4
│       S02E0007.mp4
│       S02E0008.mp4
│       S02E0009.mp4
│       S02E0010.mp4
│       S02E0011.mp4
│       S02E0012.mp4
│       S02E0013.mp4
│
└───Season 3
        S03E0001.mp4
        S03E0002.mp4
        S03E0003.mp4
        S03E0004.mp4
        S03E0005.mp4
        S03E0006.mp4
        S03E0007.mp4
        S03E0008.mp4
        S03E0009.mp4
        S03E0010.mp4
        S03E0011.mp4
        S03E0012.mp4
        S03E13A.mp4
        S03E13B.mp4
        S03E13C.mp4
```

</details>

<details>
  <summary>Rurouni Kenshin</summary>

```
├───Season 1
│       S01E0001.mp4
│       S01E0002.mp4
│       S01E0003.mp4
│       S01E0004.mp4
│       S01E0005.mp4
│       S01E0006.mp4
│       S01E0007.mp4
│       S01E0008.mp4
│       S01E0009.mp4
│       S01E0010.mp4
│       S01E0011.mp4
│       S01E0012.mp4
│       S01E0013.mp4
│       S01E0014.mp4
│       S01E0015.mp4
│       S01E0016.mp4
│       S01E0017.mp4
│       S01E0018.mp4
│       S01E0019.mp4
│       S01E0020.mp4
│       S01E0021.mp4
│       S01E0022.mp4
│       S01E0023.mp4
│       S01E0024.mp4
│
└───Season 2
        S02E0025.mp4
        S02E0026.mp4
        S02E0027.mp4
        S02E0028.mp4
        S02E0029.mp4
        S02E0030.mp4
        S02E0031.mp4
```

</details>

<details>
  <summary>That Time I Got Reincarnated As A Slime</summary>

```
├───Season 1
│       S01E0001.mp4
│       S01E0002.mp4
│       S01E0003.mp4
│       S01E0004.mp4
│       S01E0005.mp4
│       S01E0006.mp4
│       S01E0007.mp4
│       S01E0008.mp4
│       S01E0009.mp4
│       S01E0010.mp4
│       S01E0011.mp4
│       S01E0012.mp4
│       S01E0013.mp4
│       S01E0014.mp4
│       S01E0015.mp4
│       S01E0016.mp4
│       S01E0017.mp4
│       S01E0018.mp4
│       S01E0019.mp4
│       S01E0020.mp4
│       S01E0021.mp4
│       S01E0022.mp4
│       S01E0023.mp4
│       S01E0024.5.mp4
│       S01E0024.mp4
│
├───Season 2
│       S02E0024.9.mp4
│       S02E0025.mp4
│       S02E0026.mp4
│       S02E0027.mp4
│       S02E0028.mp4
│       S02E0029.mp4
│       S02E0030.mp4
│       S02E0031.mp4
│       S02E0032.mp4
│       S02E0033.mp4
│       S02E0034.mp4
│       S02E0035.mp4
│       S02E0036.5.mp4
│       S02E0036.mp4
│       S02E0037.mp4
│       S02E0038.mp4
│       S02E0039.mp4
│       S02E0040.mp4
│       S02E0041.mp4
│       S02E0042.mp4
│       S02E0043.mp4
│       S02E0044.mp4
│       S02E0045.mp4
│       S02E0046.mp4
│       S02E0047.mp4
│       S02E0048.mp4
│
├───Season 3
│       S03E0048.5.mp4
│       S03E0049.mp4
│       S03E0050.mp4
│       S03E0051.mp4
│       S03E0052.mp4
│       S03E0053.mp4
│       S03E0054.mp4
│       S03E0055.mp4
│       S03E0056.mp4
│       S03E0057.mp4
│       S03E0058.mp4
│       S03E0059.mp4
│       S03E0060.mp4
│       S03E0061.mp4
│       S03E0062.mp4
│       S03E0063.mp4
│       S03E0064.mp4
│       S03E0065.5.mp4
│       S03E0065.mp4
│       S03E0066.mp4
│       S03E0067.mp4
│       S03E0068.mp4
│       S03E0069.mp4
│       S03E0070.mp4
│       S03E0071.mp4
│       S03E0072.mp4
│
├───That Time I Got Reincarnated as a Slime OAD
│       E01.mp4
│       E02.mp4
│       E03.mp4
│       E04.mp4
│       E05.mp4
│
├───That Time I Got Reincarnated as a Slime the Movie Scarlet Bond
│       That Time I Got Reincarnated as a Slime the Movie Scarlet Bond.mp4
│
└───That Time I Got Reincarnated as a Slime Visions of Coleus
        E01.mp4
        E02.mp4
        E03.mp4
```

</details>

<details>
  <summary>Attack on Titan</summary>

```
├───Attack on Titan OADs
│       E01.mp4
│       E02.mp4
│       E03.mp4
│       E04.mp4
│       E05.mp4
│       E06.mp4
│       E07.mp4
│       E08.mp4
│
├───Season 2
│       S02E0026.mp4
│       S02E0027.mp4
│       S02E0028.mp4
│       S02E0029.mp4
│       S02E0030.mp4
│       S02E0031.mp4
│       S02E0032.mp4
│       S02E0033.mp4
│       S02E0034.mp4
│       S02E0035.mp4
│       S02E0036.mp4
│       S02E0037.mp4
│
├───Season 3
│       S03E0038.mp4
│       S03E0039.mp4
│       S03E0040.mp4
│       S03E0041.mp4
│       S03E0042.mp4
│       S03E0043.mp4
│       S03E0044.mp4
│       S03E0045.mp4
│       S03E0046.mp4
│       S03E0047.mp4
│       S03E0048.mp4
│       S03E0049.mp4
│       S03E0050.mp4
│       S03E0051.mp4
│       S03E0052.mp4
│       S03E0053.mp4
│       S03E0054.mp4
│       S03E0055.mp4
│       S03E0056.mp4
│       S03E0057.mp4
│       S03E0058.mp4
│       S03E0059.mp4
│
└───Season 4
        S04E-SP1.mp4
        S04E-SP2.mp4
        S04E0060.mp4
        S04E0061.mp4
        S04E0062.mp4
        S04E0063.mp4
        S04E0064.mp4
        S04E0065.mp4
        S04E0066.mp4
        S04E0067.mp4
        S04E0068.mp4
        S04E0069.mp4
        S04E0070.mp4
        S04E0071.mp4
        S04E0072.mp4
        S04E0073.mp4
        S04E0074.mp4
        S04E0075.mp4
        S04E0076.mp4
        S04E0077.mp4
        S04E0078.mp4
        S04E0079.mp4
        S04E0080.mp4
        S04E0081.mp4
        S04E0082.mp4
        S04E0083.mp4
        S04E0084.mp4
        S04E0085.mp4
        S04E0086.mp4
        S04E0087.mp4
```

</details>

### Select Metadata Language
The plugin uses the metadata language that was set in Jellyfin. Refer to the Jellyfin documentation.

### Add a Crunchyroll reference/id manually
If a series could not be found via crunchyroll search, but you find it via Google or other search engine <br>
then you can add it manually by editing the metadata of the series.
1. Go and find the anime on Crunchyroll.
2. Copy the Crunchyroll id from the url `https://www.crunchyroll.com/series/<id>/one-piece`
3. Edit the metadata of your anime in the jellyfin ui
4. You will find a textarea, in the "External IDs" section with the name "Crunchyroll Series Id" (it's the first one, if there are two)
5. Paste the id into the textarea and save
6. Run a library scan

Or extend the folder name of your series with `[CrunchyrollId-<id>]` like tvdbid <br> https://jellyfin.org/docs/general/server/media/shows/

## Build

Install the dotnet 8 sdk and run `dotnet build` 
(To copy the binaries automatically to the local plugins folder add the following code as post-build-event (windows only)
```
if $(ConfigurationName) == Debug (
 xcopy /y "$(TargetDir)*.*" "%localAppData%/jellyfin/plugins/Crunchyroll"
)
```

E2E tests:
Add this as post build event, to copy automatically changes from source code to the docker image
```
rd /s /q "$(TargetDir)plugin"
mkdir $(TargetDir)plugin
xcopy /y "$(SolutionDir)src/Jellyfin.Plugin.Crunchyroll/$(OutDir)*.*" $(TargetDir)plugin
```

#### Adding Code First EF-Migrations
1. Remove `<IncludeAssets>compile</IncludeAssets>` from the Entity Framework Nuget Packages & `Jellyfin.Controller & Jellyfin.Model` in `Jellyfin.Plugin.Crunchyroll.csproj` <br>
Or just remove the nuget packages with a package manager and re-add them. (ef-tool needs the runtime binaries) <br>
2. Run `dotnet ef migrations add <migrationname>` in `/src/Jellyfin.Plugin.Crunchyroll`
3. Add `<IncludeAssets>compile</IncludeAssets>` back to the Entity Framework Nuget Packages, otherwise running this plugin on `Jellyfin.Server` will result in errors/dependency conflicts 
