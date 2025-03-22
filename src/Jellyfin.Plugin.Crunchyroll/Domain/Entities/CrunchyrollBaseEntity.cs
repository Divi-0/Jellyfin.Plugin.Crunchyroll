using System;

namespace Jellyfin.Plugin.Crunchyroll.Domain.Entities;

public abstract record CrunchyrollBaseEntity
{
    public DateTime LastUpdatedAt { get; set; }
}