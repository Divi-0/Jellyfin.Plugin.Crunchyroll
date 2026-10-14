using System.Text.RegularExpressions;
using Bogus;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static partial class RandomizerExtensions
{
    /// <summary>
    /// Like <see cref="Randomizer.Words"/>, but only with letters, digits, spaces and hyphens. Bogus can return words
    /// like "Falkland Islands (Malvinas)", "24/7" or "U.S.", which collide with the parsing of folder and title names
    /// (bracket attributes, path separators, file extensions).
    /// </summary>
    public static string FileNameSafeWords(this Randomizer randomizer, int? count = null)
    {
        var words = UnsafeCharactersRegex().Replace(randomizer.Words(count), string.Empty);
        return MultipleSpacesRegex().Replace(words, " ").Trim();
    }

    [GeneratedRegex(@"[^\p{L}\p{N} \-]")]
    private static partial Regex UnsafeCharactersRegex();

    [GeneratedRegex(" {2,}")]
    private static partial Regex MultipleSpacesRegex();
}