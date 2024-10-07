using AutoFixture;
using Jellyfin.Plugin.Crunchyroll.Features.WaybackMachine.Client.Dto;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Fixture;

public class WaybackMachineAvailabilityResponseCustomization : ICustomization
{
    private readonly string _mockUrl;

    public WaybackMachineAvailabilityResponseCustomization(string mockUrl)
    {
        _mockUrl = mockUrl;
    }
    
    public void Customize(IFixture fixture)
    {
        fixture.Register<ClosestSnapshot>(() => Customized(fixture));
    }
    
    private ClosestSnapshot Customized(IFixture fixture)
    {
        var closestSnapshot = fixture.Build<ClosestSnapshot>()
            .With(x => x.Url, $"{_mockUrl}{Guid.NewGuid()}")
            .Create();
        
        return closestSnapshot;
    }
}