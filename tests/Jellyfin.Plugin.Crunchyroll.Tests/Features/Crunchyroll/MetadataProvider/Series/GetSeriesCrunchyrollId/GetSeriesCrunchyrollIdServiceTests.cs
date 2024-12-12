using System.Globalization;
using AutoFixture;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId.Client;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId;

public class GetSeriesCrunchyrollIdServiceTests
{
    private readonly ICrunchyrollSeriesIdClient _crunchyrollClientMock;
    private readonly ILoginService _loginService;
    
    private readonly GetSeriesCrunchyrollIdService _sut;

    private readonly Fixture _fixture;

    public GetSeriesCrunchyrollIdServiceTests()
    {
        _fixture = new Fixture();

        _crunchyrollClientMock = Substitute.For<ICrunchyrollSeriesIdClient>();
        _loginService = Substitute.For<ILoginService>();

        _sut = new GetSeriesCrunchyrollIdService(_crunchyrollClientMock, _loginService);
    }

    [Fact]
    public async Task ReturnsTitleId_WhenTitleIsUsedToGetTitleId_GivenTitle()
    {
        //Arrange
        var title = _fixture.Create<string>();
        var titleId = CrunchyrollIdFaker.Generate();
        var language = new CultureInfo("en-US");
        
        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _crunchyrollClientMock
            .GetSeriesIdAsync(title, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(titleId);

        //Act
        var result = await _sut.GetSeriesCrunchyrollId(title, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().Be(titleId);

        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsError_WhenCrunchyrollRequestFails_GivenTitle()
    {
        //Arrange
        var title = _fixture.Create<string>();
        const string errorcode = "error";
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        _crunchyrollClientMock
            .GetSeriesIdAsync(title, Arg.Any<CultureInfo>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Fail<CrunchyrollId?>(errorcode)));

        //Act
        var result = await _sut.GetSeriesCrunchyrollId(title, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == errorcode);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailed_WhenLoginFails_GivenTitle()
    {
        //Arrange
        var title = _fixture.Create<string>();
        const string errorcode = "error123";
        var language = new CultureInfo("en-US");

        _loginService
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail(errorcode));

        //Act
        var result = await _sut.GetSeriesCrunchyrollId(title, language, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Message == errorcode);
        
        await _loginService
            .Received(1)
            .LoginAnonymouslyAsync(Arg.Any<CancellationToken>());

        await _crunchyrollClientMock
            .DidNotReceive()
            .GetSeriesIdAsync(title, language, Arg.Any<CancellationToken>());
    }
}