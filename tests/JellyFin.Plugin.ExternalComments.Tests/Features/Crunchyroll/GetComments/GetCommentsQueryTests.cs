using AutoFixture;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments.Client;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.GetComments
{
    public class GetCommentsQueryTests
    {
        private readonly ILogger<GetCommentsQueryHandler> _loggerMock;
        private readonly ICrunchyrollGetCommentsClient _crunchyrollClientMock;
        private readonly ILibraryManager _libraryManagerMock;

        private GetCommentsQueryHandler _sut;

        private readonly Fixture _fixture;

        public GetCommentsQueryTests()
        {
            _fixture = new Fixture();

            _loggerMock = Substitute.For<ILogger<GetCommentsQueryHandler>>();
            _crunchyrollClientMock = Substitute.For<ICrunchyrollGetCommentsClient>();
            _libraryManagerMock = Substitute.For<ILibraryManager>();

            _sut = new GetCommentsQueryHandler(
                _loggerMock, 
                _crunchyrollClientMock, 
                _libraryManagerMock
            );
        }

        [Fact]
        public async Task ReturnsPaginatedComments_WhenTitleIdIsUsedToGetComments_GivenTitleId()
        {
            //Arrange
            var titleId = Guid.NewGuid().ToString();
            var parentId = Guid.NewGuid();
            const int pageNumber = 1;
            const int pageSize = 10;

            _libraryManagerMock
                .GetItemById(parentId)
                .Returns(new Series()
                {
                    ProviderIds = new Dictionary<string, string>
                    {
                        { CrunchyrollExternalKeys.Id, titleId }
                    }
                });

            _libraryManagerMock
                .GetItemsResult(Arg.Is<InternalItemsQuery>(x => x.AncestorWithPresentationUniqueKey == titleId))
                .Returns(new MediaBrowser.Model.Querying.QueryResult<BaseItem>()
                {
                    Items = new List<BaseItem>()
                    {
                        new Series()
                        {
                            ParentId = parentId
                        }
                    }
                });

            _crunchyrollClientMock
                .GetCommentsAsync(titleId, pageNumber, pageSize, Arg.Any<CancellationToken>())
                .Returns(new CommentsResponse());

            BaseItem.LibraryManager = _libraryManagerMock;

            //Act
            var query = new GetCommentsQuery(titleId, pageNumber, pageSize);
            var result = await _sut.Handle(query, CancellationToken.None);

            //Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }
    }
}
