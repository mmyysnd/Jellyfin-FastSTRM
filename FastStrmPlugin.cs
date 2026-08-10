using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FastSTRM
{
    public class FastStrmPlugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "FastSTRM";

        public override Guid Id => Guid.Parse("7b2049e3-8551-409d-8c11-92b1552b7156");

        public override string Description => "Bypasses PlaybackInfo probe for .strm files to eliminate starting delay.";

        public FastStrmPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static FastStrmPlugin? Instance { get; private set; }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
    }
}
