﻿using ElectronNET.API;
using Microsoft.JSInterop;
using System;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    internal class ElectronJSRuntime : JSRuntimeBase
    {
        private readonly BrowserWindow _window;

        public ElectronJSRuntime(BrowserWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            ElectronNET.API.Electron.IpcMain.Send(_window, "JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }
    }
}
