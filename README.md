## About

This plugin is a third party plugin and was not created by the official Jellyfin team.
It collects metadata from Crunchyroll and sets Description, Images, ... to the existing Jellyfin Items.
It also brings back reviews and comments, in read-only mode. (Scraped from wayback-machine)

## Build

Install the dotnet 8 sdk and run `dotnet build` 
(To copy the binaries automatically to the local plugins folder add the following code as post-build-event (windows only)
```
if $(ConfigurationName) == Debug-Copy (
 xcopy /y "$(TargetDir)/*.*" "%localAppData%/jellyfin/plugins/CrunchyrollPlugin"
)
```

## Installation
1. In the Jellyfin Dashboard select the `Plugins -> Repositories` Tab and add the manifest `https://raw.githubusercontent.com/Divi-0/Jellyfin.Plugin.Crunchyroll/refs/heads/main/manifest.json`
2. Go to the `Catalog` and install `"CrunchyrollPlugin"`
3. The Requests to the Crunchyroll API need to bypass a bot detection. Install FlareSolverr via https://github.com/FlareSolverr/FlareSolverr?tab=readme-ov-file#installation
4. Go to the configuration page of the "CrunchyrollPlugin" plugin ``Plugins -> My Plugins -> CrunchyrollPlugin``
5. Enter the FlareSolverr URL (Example: `http://localhost:1234`)
6. Optional but recommended: Enter the path with your anime collection you want to scan (Example: `/mnt/Anime`)