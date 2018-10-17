// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using ElectronNET.API;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.Server;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    internal class ElectronRenderer : Renderer
    {
        private readonly BrowserWindow _window;
        public int RendererId { get; }

        static ElectronRenderer()
        {
            var resolverType = typeof(BlazorHub).Assembly
                .GetType("Microsoft.AspNetCore.Blazor.Server.Circuits.RenderBatchFormatterResolver", true);
            var resolver = (IFormatterResolver)Activator.CreateInstance(resolverType);
            CompositeResolver.RegisterAndSetAsDefault(resolver, StandardResolver.Instance);
        }

        public ElectronRenderer(IServiceProvider serviceProvider, BrowserWindow window)
            : base(serviceProvider)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            RendererId = RendererRegistry.Current.Add(this);
        }

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Attaches a new root component to the renderer,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public void AddComponent<TComponent>(string domElementSelector)
            where TComponent: IComponent
        {
            AddComponent(typeof(TComponent), domElementSelector);
        }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="BrowserRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public void AddComponent(Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            var attachComponentTask = JSRuntime.Current.InvokeAsync<object>(
                "Blazor._internal.attachRootComponentToElement",
                RendererId,
                domElementSelector,
                componentId);
            CaptureAsyncExceptions(attachComponentTask);

            RenderRootComponent(componentId);
        }

        /// <inheritdoc />
        protected override void UpdateDisplay(in RenderBatch batch)
        {
            var bytes = MessagePackSerializer.Serialize(batch);
            var base64 = Convert.ToBase64String(bytes);
            ElectronNET.API.Electron.IpcMain.Send(_window, "JS.RenderBatch", RendererId, base64);
        }

        private void CaptureAsyncExceptions(Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    UnhandledException?.Invoke(this, t.Exception);
                }
            });
        }
    }
}
