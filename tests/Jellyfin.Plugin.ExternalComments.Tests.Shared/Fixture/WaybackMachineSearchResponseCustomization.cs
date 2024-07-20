using AutoFixture;

namespace Jellyfin.Plugin.ExternalComments.Tests.Shared.Fixture;

public class WaybackMachineSearchResponseCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register<Features.WaybackMachine.Client.Dto.SearchResponse>(() => Create(fixture));
    }
    
    private static Features.WaybackMachine.Client.Dto.SearchResponse Create(IFixture fixture)
    {
        var searchResponse = fixture.Build<Features.WaybackMachine.Client.Dto.SearchResponse>()
            .With(x => x.MimeType, "text/html")
            .With(x => x.Status, "200")
            .Create();
        
        return searchResponse;
    }
}