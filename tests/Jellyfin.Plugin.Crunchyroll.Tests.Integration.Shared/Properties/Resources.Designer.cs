﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Jellyfin.Plugin.Crunchyroll.Tests.Integration.Shared.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!DOCTYPE html&gt;&lt;html lang=&quot;de&quot; dir=&quot;ltr&quot; data-react-helmet=&quot;lang&quot;&gt;&lt;head&gt;&lt;script src=&quot;//archive.org/includes/athena.js&quot; type=&quot;text/javascript&quot;&gt;&lt;/script&gt;
        ///    &lt;script type=&quot;text/javascript&quot;&gt;window.addEventListener(&apos;DOMContentLoaded&apos;,function(){var v=archive_analytics.values;v.service=&apos;wb&apos;;v.server_name=&apos;wwwb-app202.us.archive.org&apos;;v.server_ms=1178;archive_analytics.send_pageview({});});&lt;/script&gt;
        ///    &lt;script type=&quot;text/javascript&quot; src=&quot;https://web-static.archive.org/_static/js/bundle-playback.js?v=HxkREWBo&quot; c [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CrunchyrollEpisodeHtml {
            get {
                return ResourceManager.GetString("CrunchyrollEpisodeHtml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!DOCTYPE html&gt;&lt;html lang=&quot;de&quot; dir=&quot;ltr&quot; data-react-helmet=&quot;lang&quot;&gt;&lt;head&gt;&lt;script type=&quot;text/javascript&quot; src=&quot;https://web-static.archive.org/_static/js/bundle-playback.js?v=qM_6omlu&quot; charset=&quot;utf-8&quot;&gt;&lt;/script&gt;
        ///&lt;script type=&quot;text/javascript&quot; src=&quot;https://web-static.archive.org/_static/js/wombat.js?v=txqj7nKC&quot; charset=&quot;utf-8&quot;&gt;&lt;/script&gt;
        ///&lt;script&gt;window.RufflePlayer=window.RufflePlayer||{};window.RufflePlayer.config={&quot;autoplay&quot;:&quot;on&quot;,&quot;unmuteOverlay&quot;:&quot;hidden&quot;};&lt;/script&gt;
        ///&lt;script type=&quot;text/javascript&quot; src=&quot;https:// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CrunchyrollTitleHtml {
            get {
                return ResourceManager.GetString("CrunchyrollTitleHtml", resourceCulture);
            }
        }
    }
}