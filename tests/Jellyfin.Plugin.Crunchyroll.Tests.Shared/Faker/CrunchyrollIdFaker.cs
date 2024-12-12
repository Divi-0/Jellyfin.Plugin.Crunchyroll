using Jellyfin.Plugin.Crunchyroll.Domain;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker
{
    public static class CrunchyrollIdFaker
    {
        public static CrunchyrollId Generate()
        {
            return new Bogus.Faker().Random.AlphaNumeric(9).ToUpper();
        }
    }
}
