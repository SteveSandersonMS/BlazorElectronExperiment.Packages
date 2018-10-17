using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    public static class BlazorElectron
    {
        public static void Run<TStartup>(string path)
        {
            Launcher.StartElectronProcess(async () =>
            {
                var window = await Launcher.CreateWindowAsync(path);
                JSRuntime.SetCurrentJSRuntime(Launcher.ElectronJSRuntime);

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton<IUriHelper>(ElectronUriHelper.Instance);
                serviceCollection.AddSingleton<IJSRuntime>(Launcher.ElectronJSRuntime);

                var startup = new ConventionBasedStartup(Activator.CreateInstance(typeof(TStartup)));
                startup.ConfigureServices(serviceCollection);

                var services = serviceCollection.BuildServiceProvider();
                var builder = new ElectronBlazorApplicationBuilder(services);
                startup.Configure(builder, services);

                ElectronUriHelper.Instance.Initialize(
                    Launcher.InitialUriAbsolute,
                    Launcher.BaseUriAbsolute);

                var renderer = new ElectronRenderer(services, window);
                foreach (var rootComponent in builder.Entries)
                {
                    renderer.AddComponent(rootComponent.componentType, rootComponent.domElementSelector);
                }
            });
        }
    }
}
