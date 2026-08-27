using MediaBrowser.Model.Plugins;

namespace FastSTRM.Configuration;

/// <summary>
/// Plugin configuration model for FastSTRM.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically probe
    /// newly added .strm items that lack stream info.
    /// </summary>
    public bool EnableAutoProbe { get; set; }
}
