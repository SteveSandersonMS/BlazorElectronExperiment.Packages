using Microsoft.AspNetCore.Blazor.Builder;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    internal class ElectronBlazorApplicationBuilder : IBlazorApplicationBuilder
    {
        public ElectronBlazorApplicationBuilder(IServiceProvider services)
        {
            Services = services;
            Entries = new List<(Type componentType, string domElementSelector)>();
        }

        public List<(Type componentType, string domElementSelector)> Entries { get; }

        public IServiceProvider Services { get; }

        public void AddComponent(Type componentType, string domElementSelector)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (domElementSelector == null)
            {
                throw new ArgumentNullException(nameof(domElementSelector));
            }

            Entries.Add((componentType, domElementSelector));
        }
    }
}
