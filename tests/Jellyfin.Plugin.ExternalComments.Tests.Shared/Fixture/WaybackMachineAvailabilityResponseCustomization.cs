using AutoFixture;
using Jellyfin.Plugin.ExternalComments.Features.WaybackMachine.Client.Dto;

namespace Jellyfin.Plugin.ExternalComments.Tests.Shared.Fixture;

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