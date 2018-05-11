using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    public abstract class InteropChannel : IDisposable
    {
        public static InteropChannel Create(BrowserWindow window, SynchronizationContext synchronizationContext)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (synchronizationContext == null)
            {
                throw new ArgumentNullException(nameof(synchronizationContext));
            }

            return new ElectronInteropChannel(window, synchronizationContext);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        // TODO: Currently no one calls this, so it will live forever
        public void Dispose()
        {
            Dispose(true);
        }

        private class ElectronInteropChannel : InteropChannel
        {
            private readonly BrowserWindow _window;
            private readonly SynchronizationContext _synchronizationContext;

            public ElectronInteropChannel(BrowserWindow window, SynchronizationContext synchronizationContext)
            {
                _window = window;
                _synchronizationContext = synchronizationContext;

                ElectronNET.API.Electron.IpcMain.OnSync("blazor:CallDotNetFromJS", (obj) =>
                {
                    DotnetInvokeResponse response = null;
                    _synchronizationContext.Send((_) =>  { response = DispatchSync(_); }, obj);

                    Debug.Assert(response != null);
                    return JsonConvert.SerializeObject(response);
                });
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    ElectronNET.API.Electron.IpcMain.RemoveAllListeners("blazor:CallDotNetFromJS");
                }
            }

            private DotnetInvokeResponse DispatchSync(object obj)
            {
                var message = ((JObject)obj).ToObject<DotNetInvokeMessage>();
                var method = GetMethod(message.MethodInfo);
                var args = PrepareArguments(method, message.ArgsJsonArray);

                try
                {
                    var result = method.Invoke(message.Target, args);
                    return new DotnetInvokeResponse() { ResultJson = JsonConvert.SerializeObject(result), };
                }
                catch (Exception ex)
                {
                    return new DotnetInvokeResponse() { Exception = ex.ToString(), };
                }
            }

            private static MethodInfo GetMethod(DotNetInvokeMethodInfo info)
            {
                var assembly = Assembly.Load(info.AssemblyName);
                var type = assembly.GetType($"{info.Namespace}.{info.ClassName}", throwOnError: true);
                var method = type.GetMethod(info.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                return method;
            }

            private static object[] PrepareArguments(MethodInfo method, string[] argsJson)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    args[i] = JsonConvert.DeserializeObject(argsJson[i], parameters[i].ParameterType);
                }

                return args;
            }
        }
    }
}
