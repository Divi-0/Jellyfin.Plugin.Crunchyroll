using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.Crunchyroll.Common;

public class CrunchyrollPluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHostedService<IndexHtmlInjectionHostedService>();
    }
}

public sealed record PatchRequestPayload
{
    [JsonPropertyName("contents")]
    public string? Contents { get; set; }
}

public class IndexHtmlInjectionHostedService : IHostedService
{
    private const string ScriptHtmlElement = $"<script src=\"/{Routes.Root}/Script\"></script>";

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<IndexHtmlInjectionHostedService> _logger;

    public IndexHtmlInjectionHostedService(
        IApplicationPaths applicationPaths,
        ILogger<IndexHtmlInjectionHostedService> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (TryRegisterWithFileTransformation())
        {
            return Task.CompletedTask;
        }

        var config = CrunchyrollPlugin.Instance?.Configuration;
        if (config is not null && (config.IsFeatureReviewsEnabled || config.IsFeatureCommentsEnabled))
        {
            InjectScriptIntoIndexFileFallback();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static string TransformIndexHtml(PatchRequestPayload payload)
    {
        var contents = payload.Contents ?? string.Empty;
        return InsertScriptBeforeBodyEnd(contents) ?? contents;
    }

    private bool TryRegisterWithFileTransformation()
    {
        try
        {
            var fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(context => context.Assemblies)
                .FirstOrDefault(assembly =>
                    assembly.GetName().Name == "Jellyfin.Plugin.FileTransformation");

            var pluginInterfaceType = fileTransformationAssembly?
                .GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");

            var registerMethod = pluginInterfaceType?.GetMethod("RegisterTransformation", [typeof(JObject)]);

            if (registerMethod is null)
            {
                return false;
            }

            var pluginId = CrunchyrollPlugin.PluginId;
            var payload = new JObject
            {
                ["id"] = pluginId,
                ["fileNamePattern"] = "index\\.html$",
                ["callbackAssembly"] = typeof(IndexHtmlInjectionHostedService).Assembly.FullName,
                ["callbackClass"] = typeof(IndexHtmlInjectionHostedService).FullName,
                ["callbackMethod"] = nameof(TransformIndexHtml)
            };

            registerMethod.Invoke(null, [payload]);
            _logger.LogInformation("Registered Crunchyroll script injection with File Transformation plugin");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to register index.html transformation with File Transformation plugin");
            return false;
        }
    }

    private void InjectScriptIntoIndexFileFallback()
    {
        var indexFilePath = Path.Combine(_applicationPaths.WebPath, "index.html");

        try
        {
            var indexFileContent = File.ReadAllText(indexFilePath);
            var updatedContent = InsertScriptBeforeBodyEnd(indexFileContent);

            if (updatedContent is not null)
            {
                File.WriteAllText(indexFilePath, updatedContent);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e,
                "Could not update {IndexFilePath}. Install the File Transformation plugin or grant write permissions to enable the Crunchyroll UI script.",
                indexFilePath);
        }
    }

    private static string? InsertScriptBeforeBodyEnd(string html)
    {
        if (string.IsNullOrEmpty(html) || html.Contains(ScriptHtmlElement, StringComparison.Ordinal))
        {
            return null;
        }

        var lastIndexOfBody = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return lastIndexOfBody > 0 ? html.Insert(lastIndexOfBody, ScriptHtmlElement) : null;
    }
}
