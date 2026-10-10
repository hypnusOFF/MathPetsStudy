using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.Components.WebView;
using MathPet.UI.Components;
using MathPet.Core;
namespace MathPet.Android;
public sealed class MainPage:ContentPage
{
    public MainPage()
    {
        BackgroundColor=Color.FromArgb("#F8F7FB");
        var webView=new BlazorWebView { HostPage="wwwroot/index.html" };
        webView.BlazorWebViewInitialized += (_, e) =>
        {
            // Embedded B站 uses its own anonymous session cookies, like a browser.
            global::Android.Webkit.CookieManager.Instance?.SetAcceptThirdPartyCookies(e.WebView,true);
        };
        webView.UrlLoading += (_, e) =>
        {
            if(TeachingResources.IsPlayerNavigation(e.Url))
                e.UrlLoadingStrategy=UrlLoadingStrategy.OpenInWebView;
            else if(e.Url.Scheme is not ("http" or "https")||e.UrlLoadingStrategy==UrlLoadingStrategy.OpenExternally)
                e.UrlLoadingStrategy=UrlLoadingStrategy.CancelLoad;
        };
        webView.RootComponents.Add(new RootComponent { Selector="#app",ComponentType=typeof(GameApp) });
        Content=webView;
    }
}
