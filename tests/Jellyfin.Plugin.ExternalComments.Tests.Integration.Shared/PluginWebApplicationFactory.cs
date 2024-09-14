using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace Jellyfin.Plugin.ExternalComments.Tests.Integration.Shared;

public class PluginWebApplicationFactory : WebApplicationFactory<Program>
{
    public static PluginWebApplicationFactory Instance { get; private set; } = null!;
    public static MockHttpMessageHandler CrunchyrollHttpMessageHandlerMock { get; private set; } = null!;
    public static ILibraryManager LibraryManagerMock { get; private set; } = null!;
    public static IItemRepository ItemRepositoryMock { get; private set; } = null!;

    public PluginWebApplicationFactory()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        CrunchyrollHttpMessageHandlerMock = mockHttpMessageHandler;
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSingleton<IApplicationPaths>(Substitute.For<IApplicationPaths>());
            LibraryManagerMock = Substitute.For<ILibraryManager>();
            ItemRepositoryMock = Substitute.For<IItemRepository>();
            BaseItem.LibraryManager = LibraryManagerMock;
            BaseItem.ItemRepository = ItemRepositoryMock;
            services.AddSingleton<ILibraryManager>(LibraryManagerMock);
            services.AddSingleton<IItemRepository>(ItemRepositoryMock);

            var xmlSerializer = Substitute.For<IXmlSerializer>();
            xmlSerializer.DeserializeFromFile(Arg.Is(typeof(PluginConfiguration)), Arg.Any<string>())
                .Returns(new PluginConfiguration());
            services.AddSingleton<IXmlSerializer>(xmlSerializer);

            services.AddSingleton<CrunchyrollScan>();
        });
        
        builder.UseEnvironment("Development");
    }

    public static void CreateInstance()
    {
        Instance = new PluginWebApplicationFactory();
    }
}