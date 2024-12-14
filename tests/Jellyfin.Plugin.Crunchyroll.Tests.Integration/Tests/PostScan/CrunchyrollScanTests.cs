// using System.Globalization;
// using Bogus;
// using Jellyfin.Plugin.Crunchyroll.Configuration;
// using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
// using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client.Dtos;
// using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;
// using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata.Client.Dtos;
// using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;
// using Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.MockData;
// using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
// using MediaBrowser.Controller.Entities;
// using MediaBrowser.Controller.Entities.TV;
// using MediaBrowser.Controller.Library;
// using MediaBrowser.Controller.Persistence;
// using MediaBrowser.Model.Entities;
// using MediaBrowser.Model.MediaInfo;
// using Microsoft.Extensions.DependencyInjection;
// using NSubstitute;
// using NSubstitute.ClearExtensions;
// using WireMock.Client;
//
// namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Tests.PostScan;
//
// [Collection(CollectionNames.Plugin)]
// public class CrunchyrollScanTests
// {
//     private readonly WireMockFixture _wireMockFixture;
//     
//     private readonly ILibraryManager _libraryManager;
//     private readonly IWireMockAdminApi _wireMockAdminApi;
//     private readonly IItemRepository _itemRepository;
//     private readonly IMediaSourceManager _mediaSourceManager;
//     private readonly PluginConfiguration _config;
//
//     public CrunchyrollScanTests(WireMockFixture wireMockFixture)
//     {
//         _wireMockFixture = wireMockFixture;
//         
//         _libraryManager =
//             PluginWebApplicationFactory.Instance.Services.GetRequiredService<ILibraryManager>();
//         _itemRepository =
//             PluginWebApplicationFactory.Instance.Services.GetRequiredService<IItemRepository>();
//         _mediaSourceManager =
//             PluginWebApplicationFactory.Instance.Services.GetRequiredService<IMediaSourceManager>();
//         _wireMockAdminApi = wireMockFixture.AdminApiClient;
//         _config = CrunchyrollPlugin.Instance!.ServiceProvider.GetRequiredService<PluginConfiguration>();
//
//         _config.IsWaybackMachineEnabled = false;
//         _config.LibraryName = new Faker().Random.Word();
//         
//         _libraryManager.ClearSubstitute();
//         _itemRepository.ClearSubstitute();
//         _mediaSourceManager.ClearSubstitute();
//     }
//
//     [Fact]
//     public async Task SetsCrunchyrollIdsAndScrapsTitleMetadata_WhenTitlePostScanTasksAreCalled_GivenSeriesWithTitleIdAndChildren()
//     {
//         //Arrange
//         var language = new CultureInfo("en-US");
//         var seriesItems = Enumerable.Range(0, Random.Shared.Next(1, 10))
//             .Select(_ => SeriesFaker.GenerateWithTitleId())
//             .ToList();
//         
//         _libraryManager.MockCrunchyrollTitleIdScan(_itemRepository, _config.LibraryName, seriesItems);
//         
//         await _wireMockAdminApi.MockRootPageAsync();
//         await _wireMockAdminApi.MockAnonymousAuthAsync();
//
//         var seriesResponses = new Dictionary<Guid, CrunchyrollSeriesContentItem>();
//         var seriesRatingResponses = new Dictionary<Guid, CrunchyrollSeriesRatingResponse>();
//         var seasonResponses = new Dictionary<Guid, CrunchyrollSeasonsItem>();
//         foreach (var series in seriesItems)
//         {
//             var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
//                 language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//             seriesResponses.Add(series.Id, seriesResponse);
//             
//             var seriesRatingResponse = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(
//                 series.ProviderIds[CrunchyrollExternalKeys.SeriesId]);
//             seriesRatingResponses.Add(series.Id, seriesRatingResponse);
//             
//             await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//                 seriesResponse.Images.PosterTall.First().Last().Source);            
//             
//             await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//                 seriesResponse.Images.PosterWide.First().Last().Source);
//             
//             var seasons = _itemRepository.MockGetChildren(series);
//             var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse(seasons, series.ProviderIds[CrunchyrollExternalKeys.SeriesId], language.Name);
//
//             foreach (var seasonResponse in seasonsResponse.Data)
//             {
//                 var season = seasons.First(x => x.IndexNumber!.Value == seasonResponse.SeasonNumber);
//                 seasonResponses.Add(season.Id, seasonResponse);
//                 var episodes = _itemRepository.MockGetChildren(season);
//
//                 foreach (var episode in episodes)
//                 {
//                     _mediaSourceManager
//                         .GetPathProtocol(episode.Path)
//                         .Returns(MediaProtocol.File);
//                 }
//                 
//                 var crunchyrollEpisodesResponse = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse(episodes, 
//                     seasonResponse.Id, language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//
//                 foreach (var crunchyrollEpisode in crunchyrollEpisodesResponse.Data)
//                 {
//                     await _wireMockAdminApi.MockCrunchyrollEpisodeThumbnailResponse(crunchyrollEpisode);
//                 }
//             }
//         }
//         
//         //Act
//         var progress = new Progress<double>();
//         
//         //Assert
//         seriesItems.Should().AllSatisfy(series =>
//         {
//             series.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesId);
//             series.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().NotBeEmpty();
//             
//             DatabaseMockHelper.ShouldHaveMetadata( 
//                 series.ProviderIds[CrunchyrollExternalKeys.SeriesId],
//                 seriesResponses[series.Id], seriesRatingResponses[series.Id]);
//             
//             series.Name.Should().Be(seriesResponses[series.Id].Title);
//             series.Overview.Should().Be(seriesResponses[series.Id].Description);
//             series.Studios.Should().BeEquivalentTo([seriesResponses[series.Id].ContentProvider]);
//             
//             var seriesimageInfoPrimary = series.GetImageInfo(ImageType.Primary, 0);
//             seriesimageInfoPrimary.Should().NotBeNull();
//             File.Exists(seriesimageInfoPrimary.Path)
//                 .Should()
//                 .BeTrue("it should have saved the crunchyroll title poster image");
//             
//             foreach (var season in series.Children)
//             {
//                 season.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeasonId);
//                 season.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().NotBeEmpty();
//
//                 season.Name.Should().Be($"S{seasonResponses[season.Id].SeasonDisplayNumber}: {seasonResponses[season.Id].Title}");
//                 
//                 foreach (var episode in ((Season)season).Children)
//                 {
//                     episode.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
//                     episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().NotBeEmpty();
//
//                     var imageInfoPrimary = episode.GetImageInfo(ImageType.Primary, 0);
//                     imageInfoPrimary.Should().NotBeNull();
//                     File.Exists(imageInfoPrimary.Path)
//                         .Should()
//                         .BeTrue("it should have saved the crunchyroll thumbnail, as primary");
//
//                     var imageInfoThumb = episode.GetImageInfo(ImageType.Thumb, 0);
//                     imageInfoThumb.Should().NotBeNull();
//                     File.Exists(imageInfoThumb.Path)
//                         .Should()
//                         .BeTrue("it should have saved the crunchyroll thumbnail, as thumb");
//                 }
//             }
//         });
//     }
//
//     [Fact]
//     public async Task SetsCrunchyrollIdsForEpisode_WhenEpisodeHasNoEpisodeIdentifier_GivenSeriesWithTitleIdAndChildren()
//     {
//         //Arrange
//         var language = new CultureInfo("en-US");
//         var series = SeriesFaker.GenerateWithTitleId();
//         _libraryManager.MockCrunchyrollTitleIdScan(_itemRepository, _config.LibraryName, [series]);
//         
//         await _wireMockAdminApi.MockRootPageAsync();
//         await _wireMockAdminApi.MockAnonymousAuthAsync();
//         
//         var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(series.ProviderIds[CrunchyrollExternalKeys.SeriesId], language.Name, 
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//             seriesResponse.Images.PosterTall.First().Last().Source);            
//         
//         await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//             seriesResponse.Images.PosterWide.First().Last().Source);
//         
//         var season = SeasonFaker.Generate(series);
//         _itemRepository
//             .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == series.Id))
//             .Returns([season]);
//         var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([season], series.ProviderIds[CrunchyrollExternalKeys.SeriesId], language.Name);
//
//         var episode = EpisodeFaker.Generate(season);
//         const string episodeName = "abc";
//         episode.Path = $"/{new Faker().Random.Words()}/{episodeName}.mp4";
//         episode.IndexNumber = null;
//         _itemRepository
//             .GetItemList(Arg.Is<InternalItemsQuery>(x => x.ParentId == season.Id))
//             .Returns([episode]);
//         
//         _mediaSourceManager
//             .GetPathProtocol(episode.Path)
//             .Returns(MediaProtocol.File);
//         
//         var episodesResponse = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([episode], 
//             seasonsResponse.Data.First().Id, language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}",
//             new CrunchyrollEpisodesResponse
//             {
//                 Data = new []
//                 {
//                     new CrunchyrollEpisodeItem
//                     {
//                         Id = CrunchyrollIdFaker.Generate(),
//                         Title = episodeName,
//                         Description = "def",
//                         Episode = "",
//                         EpisodeNumber = null,
//                         SequenceNumber = 0,
//                         SlugTitle = CrunchyrollSlugFaker.Generate("abc"),
//                         Images = new CrunchyrollEpisodeImages(),
//                         SeasonId = CrunchyrollIdFaker.Generate(),
//                         SeriesId = CrunchyrollIdFaker.Generate(),
//                         SeriesSlugTitle= CrunchyrollSlugFaker.Generate()
//                     }
//                 }
//             });
//         
//         //Act
//         var progress = new Progress<double>();
//         
//         //Assert
//         episode.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
//         episode.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodesResponse.Data.First().Id);
//     }
//
//     [Fact]
//     public async Task SetsCrunchyrollIdsForMovie_WhenMovieFound_GivenSeriesWithTitleIdAndChildren()
//     {
//         //Arrange
//         var language = new CultureInfo("en-US");
//         var movie = MovieFaker.Generate();
//         var seasonId = CrunchyrollIdFaker.Generate();
//         var episodeId = CrunchyrollIdFaker.Generate();
//         var seriesId = CrunchyrollIdFaker.Generate();
//         
//         var movieSecond = MovieFaker.Generate();
//
//         _config.IsScanInAllLibrariesEnabled = false;
//         
//         _libraryManager.MockCrunchyrollTitleIdScanMovies(_itemRepository, _config.LibraryName, [movie]);
//         
//         await _wireMockAdminApi.MockRootPageAsync();
//         await _wireMockAdminApi.MockAnonymousAuthAsync();
//         
//         _mediaSourceManager
//             .GetPathProtocol(movie.Path)
//             .Returns(MediaProtocol.File);
//
//         await _wireMockAdminApi.MockCrunchyrollSearchResponseForMovie(
//             movie.FileNameWithoutExtension,
//             language.Name,
//             episodeId,
//             seasonId,
//             seriesId);
//         
//         var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(seriesId, language.Name, 
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         var seriesRatingResponse = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(seriesId);
//
//         var season = SeasonFaker.GenerateWithSeasonId();
//         var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([season], seriesId, language.Name);
//         var episode = EpisodeFaker.GenerateWithEpisodeId();
//         _ = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([episode], seasonsResponse.Data.First().Id, language.Name,
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         var episodeResponse = await _wireMockAdminApi.MockCrunchyrollGetEpisodeResponse(episodeId, seasonId, seriesId, 
//             language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//             episodeResponse.Data.First().Images.Thumbnail.First().Last().Source);
//         
//         //Act
//         var progress = new Progress<double>();
//         
//         //Assert
//         movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesId);
//         movie.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().Be(seriesId);
//         
//         movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
//         movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodeId);
//         
//         movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeasonId);
//         movie.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(seasonId);
//         
//         DatabaseMockHelper.ShouldHaveMetadata(seriesId, seriesResponse, seriesRatingResponse);
//         
//         //second movie
//         movieSecond.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeriesId);
//         
//         movieSecond.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.EpisodeId);
//         
//         movieSecond.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeasonId);
//     }
//
//     [Fact]
//     public async Task SetsCrunchyrollIdsForMovieInAllLibraries_WhenScanInAllLibrariesIsEnabled_GivenSeriesWithTitleIdAndChildren()
//     {
//         //Arrange
//         var language = new CultureInfo("en-US");
//         var movie = MovieFaker.Generate();
//         var seasonId = CrunchyrollIdFaker.Generate();
//         var episodeId = CrunchyrollIdFaker.Generate();
//         var seriesId = CrunchyrollIdFaker.Generate();
//         
//         var movieSecond = MovieFaker.Generate();
//         var seasonIdSecond = CrunchyrollIdFaker.Generate();
//         var episodeIdSecond = CrunchyrollIdFaker.Generate();
//         var seriesIdSecond = CrunchyrollIdFaker.Generate();
//
//         _config.IsScanInAllLibrariesEnabled = true;
//
//         _libraryManager
//             .GetItemList(Arg.Any<InternalItemsQuery>())
//             .Returns([movie, movieSecond]);
//         
//         await _wireMockAdminApi.MockRootPageAsync();
//         await _wireMockAdminApi.MockAnonymousAuthAsync();
//         
//         _mediaSourceManager
//             .GetPathProtocol(movie.Path)
//             .Returns(MediaProtocol.File);
//         
//         _mediaSourceManager
//             .GetPathProtocol(movieSecond.Path)
//             .Returns(MediaProtocol.File);
//
//         await _wireMockAdminApi.MockCrunchyrollSearchResponseForMovie(
//             movie.FileNameWithoutExtension,
//             language.Name,
//             episodeId,
//             seasonId,
//             seriesId);
//
//         await _wireMockAdminApi.MockCrunchyrollSearchResponseForMovie(
//             movieSecond.FileNameWithoutExtension,
//             language.Name,
//             episodeIdSecond,
//             seasonIdSecond,
//             seriesIdSecond);
//         
//         var seriesResponse = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(seriesId, language.Name, 
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         var seriesResponseSecond = await _wireMockAdminApi.MockCrunchyrollSeriesResponse(seriesIdSecond, language.Name, 
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         var seriesRatingResponse = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(seriesId);
//         var seriesRatingResponseSecond = await _wireMockAdminApi.MockCrunchyrollSeriesRatingResponse(seriesIdSecond);
//
//         var season = SeasonFaker.GenerateWithSeasonId();
//         var seasonsResponse = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([season], seriesId, language.Name);
//         var episode = EpisodeFaker.GenerateWithEpisodeId();
//         _ = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([episode], seasonsResponse.Data.First().Id, language.Name,
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         var episodeResponse = await _wireMockAdminApi.MockCrunchyrollGetEpisodeResponse(episodeId, seasonId, seriesId, 
//             language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//             episodeResponse.Data.First().Images.Thumbnail.First().Last().Source);
//
//         var seasonSecond = SeasonFaker.GenerateWithSeasonId();
//         var seasonsResponseSecond = await _wireMockAdminApi.MockCrunchyrollSeasonsResponse([seasonSecond], seriesIdSecond, language.Name);
//         var episodeSecond = EpisodeFaker.GenerateWithEpisodeId();
//         _ = await _wireMockAdminApi.MockCrunchyrollEpisodesResponse([episodeSecond], seasonsResponseSecond.Data.First().Id, language.Name,
//             $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         var episodeResponseSecond = await _wireMockAdminApi.MockCrunchyrollGetEpisodeResponse(episodeIdSecond, seasonIdSecond, seriesIdSecond, 
//             language.Name, $"{_wireMockFixture.Hostname}:{_wireMockFixture.MappedPublicPort}");
//         
//         await _wireMockAdminApi.MockCrunchyrollImagePosterResponse(
//             episodeResponseSecond.Data.First().Images.Thumbnail.First().Last().Source);
//         
//         //Act
//         var progress = new Progress<double>();
//         
//         //Assert
//         movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesId);
//         movie.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().Be(seriesId);
//         
//         movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
//         movie.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodeId);
//         
//         movie.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeasonId);
//         movie.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(seasonId);
//         
//         DatabaseMockHelper.ShouldHaveMetadata(seriesId, seriesResponse, seriesRatingResponse);
//         
//         //different collection
//         movieSecond.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeriesId);
//         movieSecond.ProviderIds[CrunchyrollExternalKeys.SeriesId].Should().Be(seriesIdSecond);
//         
//         movieSecond.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.EpisodeId);
//         movieSecond.ProviderIds[CrunchyrollExternalKeys.EpisodeId].Should().Be(episodeIdSecond);
//         
//         movieSecond.ProviderIds.Should().ContainKey(CrunchyrollExternalKeys.SeasonId);
//         movieSecond.ProviderIds[CrunchyrollExternalKeys.SeasonId].Should().Be(seasonIdSecond);
//         
//         DatabaseMockHelper.ShouldHaveMetadata(seriesIdSecond, seriesResponseSecond, seriesRatingResponseSecond);
//     }
//     
//     [Fact]
//     public async Task DoesNotRunScan_WhenConfigIsInvalidForLibraryName_GivenSeriesWithTitleIdAndChildren()
//     {
//         //Arrange
//         var movie = MovieFaker.Generate();
//
//         _config.IsScanInAllLibrariesEnabled = false;
//         _config.LibraryName = string.Empty;
//         
//         //Act
//         var progress = new Progress<double>();
//         await Task.Delay(500);
//         
//         //Assert
//         movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeriesId);
//         
//         movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.EpisodeId);
//         
//         movie.ProviderIds.Should().NotContainKey(CrunchyrollExternalKeys.SeasonId);
//         
//     }
// }