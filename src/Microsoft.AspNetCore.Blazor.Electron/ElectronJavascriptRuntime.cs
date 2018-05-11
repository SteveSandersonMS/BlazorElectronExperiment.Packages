using System;
using System.Collections.Generic;
using System.Linq;
using ElectronNET.API;
using Newtonsoft.Json.Linq;
using WebAssembly;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    internal class ElectronJavaScriptRuntime : IJavaScriptRuntime
    {
        const string NullValue = "__null__"; // Electron.NET's IPC doesn't accept real null as an actual arg

        private readonly BrowserWindow _window;

        public ElectronJavaScriptRuntime(BrowserWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public TRes InvokeJS<T0, T1, T2, TRes>(out string exception, string funcName, T0 arg0, T1 arg1, T2 arg2)
        {
            var argsArray = new object[] { arg0, arg1, arg2 };
            return InvokeJSArray<TRes>(out exception, funcName, argsArray);
        }

        public TRes InvokeJSArray<TRes>(out string exception, string funcName, params object[] args)
        {
            args = args ?? Array.Empty<object>();
            args = args.Select(EnsureSerializable).Prepend(funcName).ToArray();

            ElectronNET.API.Electron.IpcMain.Send(_window, "blazor:InvokeJSArray", new[] { args });

            exception = null;
            return default; // TODO: Implement synchronous calls that can return a value or catch exceptions
        }

        private static object EnsureSerializable(object value)
        {
            if (value == null)
            {
                return NullValue;
            }

            if (value is string)
            {
                return value;
            }

            if (value is int)
            {
                return value;
            }

            var bytes = InteropSerializer.Serialize(value);

            var obj = new JObject();
            obj.Add(new JProperty("kind", "msgpack"));
            obj.Add(new JProperty("bytes", bytes));
            return obj;
        }

        private static object EnsureSerializable<T>(IEnumerable<T> values)
            => values == null ? (object)NullValue : values.Select(value => EnsureSerializable(value)).ToArray();
    }
}
