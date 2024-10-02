using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Contracts.Comments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.Comments.GetComments
{
    public class GetCommentsQueryTests
    {
        private readonly ILogger<GetCommentsQueryHandler> _loggerMock;
        private readonly ICrunchyrollGetCommentsClient _crunchyrollClientMock;
        private readonly ILibraryManager _libraryManagerMock;
        private readonly ILoginService _loginService;

        private GetCommentsQueryHandler _sut;

        private readonly Fixture _fixture;

        public GetCommentsQueryTests()
        {
            _fixture = new Fixture();

            _loggerMock = Substitute.For<ILogger<GetCommentsQueryHandler>>();
            _crunchyrollClientMock = Substitute.For<ICrunchyrollGetCommentsClient>();
            _libraryManagerMock = MockHelper.LibraryManager;
            _loginService = Substitute.For<ILoginService>();

            _sut = new GetCommentsQueryHandler(
                _loggerMock, 
                _crunchyrollClientMock, 
                _libraryManagerMock,
                _loginService
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
            
            _loginService
                .LoginAnonymously(Arg.Any<CancellationToken>())
                .Returns(Result.Ok());

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
            
            await _loginService
                .Received(1)
                .LoginAnonymously(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReturnsFailed_WhenLoginFails_GivenTitleId()
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
            
            var error = Guid.NewGuid().ToString();
            _loginService
                .LoginAnonymously(Arg.Any<CancellationToken>())
                .Returns(Result.Fail(error));

            BaseItem.LibraryManager = _libraryManagerMock;

            //Act
            var query = new GetCommentsQuery(titleId, pageNumber, pageSize);
            var result = await _sut.Handle(query, CancellationToken.None);

            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(x => x.Message.Equals(error));
            
            await _loginService
                .Received(1)
                .LoginAnonymously(Arg.Any<CancellationToken>());

            await _crunchyrollClientMock
                .DidNotReceive()
                .GetCommentsAsync(titleId, pageNumber, pageSize, Arg.Any<CancellationToken>());
        }
    }
}
