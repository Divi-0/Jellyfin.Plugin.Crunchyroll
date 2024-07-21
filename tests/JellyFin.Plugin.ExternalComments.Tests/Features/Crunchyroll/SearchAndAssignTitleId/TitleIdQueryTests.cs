using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId.Client;
using NSubstitute;
using Xunit;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.SearchAndAssignTitleId
{
    public class TitleIdQueryTests
    {
        private readonly ICrunchyrollTitleIdClient _crunchyrollClientMock;
        private readonly ILoginService _loginService;
        
        private readonly TitleIdQueryHandler _sut;

        private readonly Fixture _fixture;

        public TitleIdQueryTests()
        {
            _fixture = new Fixture();

            _crunchyrollClientMock = Substitute.For<ICrunchyrollTitleIdClient>();
            _loginService = Substitute.For<ILoginService>();

            _sut = new TitleIdQueryHandler(_crunchyrollClientMock, _loginService);
        }

        [Fact]
        public async Task ReturnsTitleId_WhenTitleIsUsedToGetTitleId_GivenTitle()
        {
            //Arrange
            var title = _fixture.Create<string>();
            var titleId = Guid.NewGuid().ToString();
            var slutTitle = _fixture.Create<string>();
            
            _loginService
                .LoginAnonymously(Arg.Any<CancellationToken>())
                .Returns(Result.Ok());

            _crunchyrollClientMock
                .GetTitleIdAsync(title, Arg.Any<CancellationToken>())
                .Returns(new SearchResponse()
                {
                    Id = titleId,
                    SlugTitle = slutTitle
                });

            //Act
            var query = new TitleIdQuery(title);
            var result = await _sut.Handle(query, CancellationToken.None);

            //Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(titleId);
            result.Value.SlugTitle.Should().Be(slutTitle);

            await _loginService
                .Received(1)
                .LoginAnonymously(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReturnsError_WhenCrunchyrollRequestFails_GivenTitle()
        {
            //Arrange
            var title = _fixture.Create<string>();
            const string errorcode = "error";

            _loginService
                .LoginAnonymously(Arg.Any<CancellationToken>())
                .Returns(Result.Ok());

            _crunchyrollClientMock
                .GetTitleIdAsync(title, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Fail<SearchResponse?>(errorcode)));

            //Act
            var query = new TitleIdQuery(title);
            var result = await _sut.Handle(query, CancellationToken.None);

            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(x => x.Message == errorcode);
            
            await _loginService
                .Received(1)
                .LoginAnonymously(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReturnsFailed_WhenLoginFails_GivenTitle()
        {
            //Arrange
            var title = _fixture.Create<string>();
            const string errorcode = "error123";

            _loginService
                .LoginAnonymously(Arg.Any<CancellationToken>())
                .Returns(Result.Fail(errorcode));

            //Act
            var query = new TitleIdQuery(title);
            var result = await _sut.Handle(query, CancellationToken.None);

            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(x => x.Message == errorcode);
            
            await _loginService
                .Received(1)
                .LoginAnonymously(Arg.Any<CancellationToken>());

            await _crunchyrollClientMock
                .DidNotReceive()
                .GetTitleIdAsync(title, Arg.Any<CancellationToken>());
        }
    }
}
