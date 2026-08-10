using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FastSTRM
{
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddTransient<IConfigureOptions<MvcOptions>, MvcOptionsConfigurator>();
            serviceCollection.AddScoped<FastStrmPlaybackInfoFilter>();
        }
    }

    public class MvcOptionsConfigurator : IConfigureOptions<MvcOptions>
    {
        public void Configure(MvcOptions options)
        {
            options.Filters.Add<FastStrmPlaybackInfoFilter>();
        }
    }
}
