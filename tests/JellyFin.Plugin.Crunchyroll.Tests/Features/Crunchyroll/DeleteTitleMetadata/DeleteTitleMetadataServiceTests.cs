using System.Globalization;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.DeleteTitleMetadata;

public class DeleteTitleMetadataServiceTests
{
    private readonly IDeleteTitleMetadataRepository _repository;
    private readonly IItemRepository _itemRepository;
    
    private readonly DeleteTitleMetadataService _sut;

    public DeleteTitleMetadataServiceTests()
    {
        _repository = Substitute.For<IDeleteTitleMetadataRepository>();
        _itemRepository = MockHelper.ItemRepository;
        
        _sut = new DeleteTitleMetadataService(_repository);
    }

    [Fact]
    public async Task CallsRepository_WhenCalled_GivenId()
    {
        //Arrange
        var item = SeriesFaker.GenerateWithTitleId();

        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(i => i.ParentId == item.Id))
            .Returns([]);

        _repository
            .DeleteTitleMetadataAsync(item.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                item.GetPreferredMetadataCultureInfo())
            .Returns(Result.Ok());
        
        //Act
        await _sut.DeleteTitleMetadataAsync(item);

        //Assert
        await _repository
            .Received(1)
            .DeleteTitleMetadataAsync(item.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                item.GetPreferredMetadataCultureInfo());
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotThrow_WhenRepositoryFails_GivenId()
    {
        //Arrange
        var item = SeriesFaker.GenerateWithTitleId();
        
        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(i => i.ParentId == item.Id))
            .Returns([]);

        _repository
            .DeleteTitleMetadataAsync(item.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                item.GetPreferredMetadataCultureInfo())
            .Returns(Result.Fail("error"));
        
        //Act
        await _sut.DeleteTitleMetadataAsync(item);

        //Assert
        await _repository
            .Received(1)
            .DeleteTitleMetadataAsync(item.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                item.GetPreferredMetadataCultureInfo());

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task DeletesChilds_WhenCalled_GivenId()
    {
        //Arrange
        var folder = new Folder()
        {
            Id = Guid.NewGuid()
        };
        var item = SeriesFaker.GenerateWithTitleId();

        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(i => i.ParentId == folder.Id))
            .Returns([item]);

        _itemRepository
            .GetItemList(Arg.Is<InternalItemsQuery>(i => i.ParentId == item.Id))
            .Returns([]);

        _repository
            .DeleteTitleMetadataAsync(item.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                item.GetPreferredMetadataCultureInfo())
            .Returns(Result.Ok());
        
        //Act
        await _sut.DeleteTitleMetadataAsync(item);

        //Assert
        await _repository
            .Received(1)
            .DeleteTitleMetadataAsync(item.ProviderIds[CrunchyrollExternalKeys.SeriesId], 
                item.GetPreferredMetadataCultureInfo());
        
        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}