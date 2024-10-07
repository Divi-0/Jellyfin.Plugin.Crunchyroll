using FluentAssertions;
using Jellyfin.Plugin.Crunchyroll.Features.Script;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JellyFin.Plugin.Crunchyroll.Tests.Features.Script;

public class GetScriptControllerTests
{
    private readonly GetScriptController _sut;

    public GetScriptControllerTests()
    {
        var loggerMock = Substitute.For<ILogger<GetScriptController>>();
        
        _sut = new GetScriptController(loggerMock);
    }

    [Fact]
    public async Task ReturnsFileStream_WhenCalled_GivenGetRequest()
    {
        var response = await _sut.GetScript(CancellationToken.None);
        
        response.Should().BeOfType<FileStreamResult>();
        
        var fileStreamResult = (FileStreamResult)response;
        fileStreamResult.ContentType.Should().Be("application/javascript");
        
        using var streamReader = new StreamReader(fileStreamResult.FileStream);
        var streamContent = await streamReader.ReadToEndAsync();
        streamContent.Should().NotBeEmpty();
    }
}