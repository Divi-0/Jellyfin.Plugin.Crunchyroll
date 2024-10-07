namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker
{
    public static class CrunchyrollSlugFaker
    {
        public static string Generate()
        {
            return new Bogus.Faker().Random.Words(Random.Shared.Next(1, 5)).ToSlug();
        }

        public static string Generate(string value)
        {
            return value.ToSlug();
        }

        private static string ToSlug(this string value)
        {
            return value.ToLower().Replace(' ', '-');
        }
    }
}
