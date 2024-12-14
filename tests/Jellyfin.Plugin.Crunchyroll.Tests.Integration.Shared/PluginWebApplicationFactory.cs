using Jellyfin.Plugin.Crunchyroll.Configuration;
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

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared;

public class PluginWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    public static PluginWebApplicationFactory Instance { get; private set; } = null!;
    public static MockHttpMessageHandler CrunchyrollHttpMessageHandlerMock { get; private set; } = null!;
    public static ILibraryManager LibraryManagerMock { get; private set; } = null!;
    public static IItemRepository ItemRepositoryMock { get; private set; } = null!;
    public static IMediaSourceManager MediaSourceManagerMock { get; private set; } = null!;

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
            MediaSourceManagerMock = Substitute.For<IMediaSourceManager>();
            BaseItem.LibraryManager = LibraryManagerMock;
            BaseItem.ItemRepository = ItemRepositoryMock;
            BaseItem.MediaSourceManager = MediaSourceManagerMock;
            services.AddSingleton<ILibraryManager>(LibraryManagerMock);
            services.AddSingleton<IItemRepository>(ItemRepositoryMock);
            services.AddSingleton<IMediaSourceManager>(MediaSourceManagerMock);

            var xmlSerializer = Substitute.For<IXmlSerializer>();
            xmlSerializer.DeserializeFromFile(Arg.Is(typeof(PluginConfiguration)), Arg.Any<string>())
                .Returns(new PluginConfiguration());
            services.AddSingleton<IXmlSerializer>(xmlSerializer);
            
            
        });
        
        builder.UseEnvironment("Development");
    }

    public static void CreateInstance()
    {
        Instance = new PluginWebApplicationFactory();
    }
}