## About

This plugin is a third party plugin and was not created by the official Jellyfin team.
It collects metadata from Crunchyroll and sets Description, Images, ... to the existing Jellyfin Items.
It also brings back reviews and comments, in a read-only mode. (Scraped from WaybackMachine)

## Features
A list of all features can be found in the wiki [Features](./wiki/Features)

## Installation
A guide can be found in the [Wiki](./wiki/Installation)

## Build

Install the dotnet 8 sdk and run `dotnet build` 
(To copy the binaries automatically to the local plugins folder add the following code as post-build-event (windows only)
```
if $(ConfigurationName) == Debug-Copy (
 xcopy /y "$(TargetDir)*.*" "%localAppData%/jellyfin/plugins/Crunchyroll"
)
```