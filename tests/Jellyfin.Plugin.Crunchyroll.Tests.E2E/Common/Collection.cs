namespace Jellyfin.Plugin.Crunchyroll.Tests.E2E.Common;

public static class CollectionNames
{
    public const string E2E = "E2E";
}

[CollectionDefinition(CollectionNames.E2E)]
public class Collection : 
    ICollectionFixture<DockerNetwork>,
    ICollectionFixture<PlaywrightFixture>,
    ICollectionFixture<FlareSolverrFixture>,
    ICollectionFixture<JellyfinFixture>;