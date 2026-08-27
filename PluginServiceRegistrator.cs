using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastSTRM;

/// <summary>
/// Registers plugin services via Jellyfin's DI pipeline.
/// Decorates <see cref="IMediaSourceManager"/> with <see cref="MediaSourceInterceptor"/>
/// and registers <see cref="ProbeService"/> as a hosted singleton.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register ProbeService as singleton + hosted service for ItemAdded event lifecycle
        serviceCollection.AddSingleton<ProbeService>();
        serviceCollection.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ProbeService>());

        // Decorate IMediaSourceManager with our interceptor
        var descriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IMediaSourceManager));
        if (descriptor is null)
        {
            return;
        }

        serviceCollection.Remove(descriptor);
        serviceCollection.Add(new ServiceDescriptor(
            typeof(IMediaSourceManager),
            sp =>
            {
                // Resolve the original implementation regardless of registration style
                IMediaSourceManager inner;
                if (descriptor.ImplementationInstance is not null)
                {
                    inner = (IMediaSourceManager)descriptor.ImplementationInstance;
                }
                else if (descriptor.ImplementationFactory is not null)
                {
                    inner = (IMediaSourceManager)descriptor.ImplementationFactory(sp);
                }
                else
                {
                    inner = (IMediaSourceManager)ActivatorUtilities.CreateInstance(
                        sp, descriptor.ImplementationType!);
                }

                return new MediaSourceInterceptor(
                    inner,
                    sp.GetRequiredService<ILogger<MediaSourceInterceptor>>());
            },
            descriptor.Lifetime));
    }
}
