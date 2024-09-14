namespace Jellyfin.Plugin.ExternalComments.Tests.Shared.Faker
{
    public static class CrunchyrollIdFaker
    {
        public static string Generate()
        {
            return new Bogus.Faker().Random.AlphaNumeric(9).ToUpper();
        }
    }
}
