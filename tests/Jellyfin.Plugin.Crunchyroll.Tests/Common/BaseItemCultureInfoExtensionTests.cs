using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Common;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Common;

public class BaseItemCultureInfoExtensionTests
{
    [Theory]
    [InlineData("en", "US", "en-US")]
    [InlineData("de", "DE", "de-DE")]
    [InlineData("pt-br", "BR", "pt-BR")]
    [InlineData("es-mx", "MX", "es-MX")]
    [InlineData("zh-cn", "CN", "zh-CN")]
    public void ReturnsExpectedCultureInfo_GivenLanguageAndCountryCode(
        string metadataLanguage,
        string metadataCountryCode,
        string expectedCultureName)
    {
        // Arrange
        var info = new ItemLookupInfo
        {
            MetadataLanguage = metadataLanguage,
            MetadataCountryCode = metadataCountryCode
        };

        // Act
        var culture = info.GetPreferredMetadataCultureInfo();

        // Assert
        culture.Name.Should().Be(expectedCultureName);
    }
}
