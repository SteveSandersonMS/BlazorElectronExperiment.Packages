namespace Microsoft.AspNetCore.Blazor.Electron
{
    internal class DotNetInvokeMessage
    {
        public DotNetInvokeMethodInfo MethodInfo { get; set; }
        public object Target { get; set; }
        public string[] ArgsJsonArray { get; set; }
    }
}
