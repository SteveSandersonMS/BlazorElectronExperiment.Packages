using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Electron
{
    public class ElectronUriHelper : UriHelperBase
    {
        private static readonly string InteropPrefix = "Blazor._internal.uriHelper.";
        private static readonly string InteropEnableNavigationInterception = InteropPrefix + "enableNavigationInterception";
        private static readonly string InteropNavigateTo = InteropPrefix + "navigateTo";

        public static ElectronUriHelper Instance { get; } = new ElectronUriHelper();

        private ElectronUriHelper()
        {
        }

        public void Initialize(string uriAbsolute, string baseUriAbsolute)
        {
            SetAbsoluteBaseUri(baseUriAbsolute);
            SetAbsoluteUri(uriAbsolute);
            TriggerOnLocationChanged();

            JSRuntime.Current.InvokeAsync<object>(
                InteropEnableNavigationInterception,
                typeof(ElectronUriHelper).Assembly.GetName().Name,
                nameof(NotifyLocationChanged));
        }

        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uriAbsolute)
        {
            Instance.SetAbsoluteUri(uriAbsolute);
            Instance.TriggerOnLocationChanged();
        }

        protected override void NavigateToCore(string uri)
        {
            JSRuntime.Current.InvokeAsync<object>(InteropNavigateTo, uri);
        }
    }
}
