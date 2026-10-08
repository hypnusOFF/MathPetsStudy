using Microsoft.AspNetCore.Components.WebView.Maui;
using MathPet.UI.Components;
namespace MathPet.Android;
public sealed class MainPage:ContentPage
{
    public MainPage()
    {
        BackgroundColor=Color.FromArgb("#F8F7FB");
        var webView=new BlazorWebView { HostPage="wwwroot/index.html" };
        webView.RootComponents.Add(new RootComponent { Selector="#app",ComponentType=typeof(GameApp) });
        Content=webView;
    }
}
