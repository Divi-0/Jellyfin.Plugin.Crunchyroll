using System.Globalization;
using System.Text.Json;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Domain.Constants;
using Jellyfin.Plugin.Crunchyroll.Domain.Entities;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SetMetadataToSeries;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series;

public class SetMetadataToSeriesServiceTests
{
    private readonly PluginConfiguration _config;
    private readonly ISetMetadataToSeriesRepository _repository;
    
    private readonly SetMetadataToSeriesService _sut;

    private readonly Faker _faker;

    public SetMetadataToSeriesServiceTests()
    {
        _repository = Substitute.For<ISetMetadataToSeriesRepository>();
        _config = new PluginConfiguration
        {
            IsFeatureSeriesTitleEnabled = true,
            IsFeatureSeriesDescriptionEnabled = true,
            IsFeatureSeriesStudioEnabled = true,
            IsFeatureSeriesRatingsEnabled = true
        };
        
        _sut = new SetMetadataToSeriesService(_config, _repository);

        _faker = new Faker();
    }
    
    [Fact]
    public async Task DoesNotUpdateName_WhenFeatureSeriesTitleIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _config.IsFeatureSeriesTitleEnabled = false;

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        var newSeriesMetadataResult = await _sut.SetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        
        //Assert
        newSeriesMetadataResult.IsSuccess.Should().BeTrue();
        var newSeriesMetadata = newSeriesMetadataResult.Value;
        newSeriesMetadata.Name.Should().NotBe(titleMetadata.Title, "feature is disabled");
        newSeriesMetadata.Overview.Should().Be(titleMetadata.Description);
        newSeriesMetadata.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        newSeriesMetadata.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, Arg.Any<CultureInfo>(), 
                Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateOverview_WhenFeatureSeriesDescriptionIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _config.IsFeatureSeriesDescriptionEnabled = false;

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        var newSeriesMetadataResult = await _sut.SetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        
        //Assert
        newSeriesMetadataResult.IsSuccess.Should().BeTrue();
        var newSeriesMetadata = newSeriesMetadataResult.Value;
        newSeriesMetadata.Name.Should().Be(titleMetadata.Title);
        newSeriesMetadata.Overview.Should().NotBe(titleMetadata.Description, "feature is disabled");
        newSeriesMetadata.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        newSeriesMetadata.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateStudios_WhenFeatureSeriesStudioIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _config.IsFeatureSeriesStudioEnabled = false;

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        var newSeriesMetadataResult = await _sut.SetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        
        //Assert
        newSeriesMetadataResult.IsSuccess.Should().BeTrue();
        var newSeriesMetadata = newSeriesMetadataResult.Value;
        newSeriesMetadata.Name.Should().Be(titleMetadata.Title);
        newSeriesMetadata.Overview.Should().Be(titleMetadata.Description);
        newSeriesMetadata.Studios.Should().NotContain(titleMetadata.Studio, "feature is disabled");
        newSeriesMetadata.CommunityRating.Should().Be(titleMetadata.Rating);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DoesNotUpdateCommunityRatings_WhenFeatureSeriesRatingsIsDisabled_GivenSeriesWithTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        _config.IsFeatureSeriesRatingsEnabled = false;

        var posterTall = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };
        
        var posterWide = new ImageSource
        {
            Uri = _faker.Internet.UrlWithPath(fileExt: "jpg"),
            Height = 1,
            Width = 1
        };

        var titleMetadata = new Domain.Entities.TitleMetadata
        {
            CrunchyrollId = string.Empty,
            SlugTitle = string.Empty,
            Description = _faker.Lorem.Sentences(),
            Title = _faker.Random.Words(),
            Studio = _faker.Random.Words(),
            Rating = _faker.Random.Float(),
            PosterTall = JsonSerializer.Serialize(posterTall),
            PosterWide = JsonSerializer.Serialize(posterWide),
            Language = language.Name
        };
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleMetadata);
        
        //Act
        var newSeriesMetadataResult = await _sut.SetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        
        //Assert
        newSeriesMetadataResult.IsSuccess.Should().BeTrue();
        var newSeriesMetadata = newSeriesMetadataResult.Value;
        newSeriesMetadata.Name.Should().Be(titleMetadata.Title);
        newSeriesMetadata.Overview.Should().Be(titleMetadata.Description);
        newSeriesMetadata.Studios.Should().BeEquivalentTo([titleMetadata.Studio]);
        newSeriesMetadata.CommunityRating.Should().NotBe(titleMetadata.Rating, "feature is disabled");
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenRepositoryGetTitleMetadataFails_GivenSeriesWithTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");

        var error = Guid.NewGuid().ToString();
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(), Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(error));
        
        //Act
        var newSeriesMetadataResult = await _sut.SetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        
        //Assert
        newSeriesMetadataResult.IsFailed.Should().BeTrue();
        newSeriesMetadataResult.Errors.First().Message.Should().Be(error);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId, language, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ReturnsFailed_WhenNoTitleMetadataFound_GivenSeriesWithTitleId()
    {
        //Arrange
        var seriesCrunchyrollId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        _repository
            .GetTitleMetadataAsync(Arg.Any<CrunchyrollId>(),
                Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.TitleMetadata?)null);
        
        //Act
        var newSeriesMetadataResult = await _sut.SetSeriesMetadataAsync(seriesCrunchyrollId, language, CancellationToken.None);
        
        //Assert
        newSeriesMetadataResult.IsFailed.Should().BeTrue();
        newSeriesMetadataResult.Errors.First().Message.Should().Be(ErrorCodes.NotFound);
        
        await _repository
            .Received(1)
            .GetTitleMetadataAsync(seriesCrunchyrollId,
                language, Arg.Any<CancellationToken>());
    }
}