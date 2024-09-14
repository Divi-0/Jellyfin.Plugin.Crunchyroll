using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan
{
    internal static class PostScanServiceExtension
    {
        public static IServiceCollection AddCrunchyroll(this IServiceCollection serviceCollection, PluginConfiguration configuration)
        {
            serviceCollection.AddSingleton<IPostScanTask, SetTitleIdTask>();

            return serviceCollection;
        }
    }
}
