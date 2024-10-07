using AutoFixture;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;

public class WaybackMachineSearchResponseCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register<SearchResponse>(() => Create(fixture));
    }
    
    private static SearchResponse Create(IFixture fixture)
    {
        var searchResponse = fixture.Build<SearchResponse>()
            .With(x => x.MimeType, "text/html")
            .With(x => x.Status, "200")
            .Create();
        
        return searchResponse;
    }
}