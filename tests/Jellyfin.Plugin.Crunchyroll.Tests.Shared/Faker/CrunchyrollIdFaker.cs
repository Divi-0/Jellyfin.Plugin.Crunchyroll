namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker
{
    public static class CrunchyrollIdFaker
    {
        public static string Generate()
        {
            return new Bogus.Faker().Random.AlphaNumeric(9).ToUpper();
        }
    }
}
