using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.Components.WebView;
using MathPet.UI.Components;
namespace MathPet.Android;
public sealed class MainPage:ContentPage
{
    public MainPage()
    {
        BackgroundColor=Color.FromArgb("#F8F7FB");
        var webView=new BlazorWebView { HostPage="wwwroot/index.html" };
        webView.UrlLoading += (_, e) =>
        {
            if (e.Url.Scheme is "https" or "http" && (e.Url.Host=="bilibili.com" || e.Url.Host.EndsWith(".bilibili.com",StringComparison.OrdinalIgnoreCase)))
                e.UrlLoadingStrategy = UrlLoadingStrategy.OpenExternally;
        };
        webView.RootComponents.Add(new RootComponent { Selector="#app",ComponentType=typeof(GameApp) });
        Content=webView;
    }
}
