using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;

using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastSTRM.Injector
{
    public class WebUIInjector : IHostedService
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<WebUIInjector> _logger;

        private const string ScriptTag = @"<script src=""/FastSTRM/app.js""></script>";

        public WebUIInjector(IApplicationPaths appPaths, ILogger<WebUIInjector> logger)
        {
            _appPaths = appPaths;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var webPath = _appPaths.WebPath;
                if (string.IsNullOrEmpty(webPath))
                {
                    _logger.LogWarning("FastSTRM: WebPath is null or empty. Cannot inject script.");
                    return Task.CompletedTask;
                }

                var indexPath = Path.Combine(webPath, "index.html");
                if (!File.Exists(indexPath))
                {
                    _logger.LogWarning("FastSTRM: index.html not found at {0}", indexPath);
                    return Task.CompletedTask;
                }

                var content = File.ReadAllText(indexPath);

                if (content.Contains(ScriptTag))
                {
                    _logger.LogInformation("FastSTRM: Script already injected into index.html.");
                    return Task.CompletedTask;
                }

                // Inject right before </head> or </body>
                var insertIndex = content.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (insertIndex == -1)
                {
                    insertIndex = content.LastIndexOf("</head>", StringComparison.OrdinalIgnoreCase);
                }

                if (insertIndex != -1)
                {
                    content = content.Insert(insertIndex, ScriptTag + Environment.NewLine);
                    File.WriteAllText(indexPath, content);
                    _logger.LogInformation("FastSTRM: Successfully injected script into index.html.");
                }
                else
                {
                    _logger.LogWarning("FastSTRM: Could not find </body> or </head> in index.html.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FastSTRM: Failed to inject script into index.html.");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // We intentionally do not remove the script on stop to avoid file locking issues or race conditions,
            // and because our injected script returns 404 when the plugin is removed, causing no harm.
            return Task.CompletedTask;
        }
    }
}
