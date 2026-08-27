using System;
using System.Collections.Generic;
using FastSTRM.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace FastSTRM;

/// <summary>
/// FastSTRM plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Fixed plugin GUID.
    /// </summary>
    public const string PluginGuid = "a1b2c3d4-5678-9abc-def0-123456789abc";

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "FastSTRM";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse(PluginGuid);

    /// <inheritdoc />
    public override string Description => "消除 .strm 文件播放时的 ffprobe 延迟，实现秒开起播";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        var ns = GetType().Namespace!;
        return new[]
        {
            new PluginPageInfo
            {
                Name = "FastStrmConfigurationPage",
                EmbeddedResourcePath = ns + ".Configuration.configPage.html"
            },
            new PluginPageInfo
            {
                Name = "FastSTRM.js",
                EmbeddedResourcePath = ns + ".Configuration.configPage.js"
            }
        };
    }
}
