using Microsoft.AspNetCore.Blazor.Electron;
using Microsoft.AspNetCore.Blazor.Hosting;

namespace BlectronSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BlazorElectron.Run<Startup>("wwwroot/index.html");
        }
    }
}
