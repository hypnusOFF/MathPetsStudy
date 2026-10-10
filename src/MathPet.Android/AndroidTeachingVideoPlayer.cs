using MathPet.Core;
using MathPet.UI;
namespace MathPet.Android;

// Use an HTTPS top-level player instead of an iframe under Blazor's local origin.
public sealed class AndroidTeachingVideoPlayer : ITeachingVideoPlayer
{
    public Task OpenAsync(CourseVideo video)=>MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var navigation=Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
            ?? throw new InvalidOperationException("无法打开视频页面。");
        var player=new WebView();
        player.HandlerChanged+=(_,_)=>
        {
            if(player.Handler?.PlatformView is global::Android.Webkit.WebView native)
            {
                native.Settings.JavaScriptEnabled=true;
                native.Settings.DomStorageEnabled=true;
                // Playback follows the child's explicit selection or play-button tap.
                native.Settings.MediaPlaybackRequiresUserGesture=false;
                var cookies=global::Android.Webkit.CookieManager.Instance;
                cookies?.SetAcceptCookie(true);
                cookies?.SetAcceptThirdPartyCookies(native,true);
            }
        };
        player.Navigating+=(_,e)=>
        {
            if(!Uri.TryCreate(e.Url,UriKind.Absolute,out var uri)
                || !TeachingResources.IsPlayerNavigation(uri))
                e.Cancel=true;
        };
        var status=new Label { Text="点击播放器中的播放按钮。若仍显示 00:00，请使用浏览器播放。",FontSize=13 };
        player.Navigated+=(_,e)=>
        {
            if(e.Result!=WebNavigationResult.Success)
                status.Text="播放器加载失败，请重新加载或使用浏览器播放。";
        };
        var close=new Button { Text="返回课堂" };
        var reload=new Button { Text="重新加载" };
        var browser=new Button { Text="用浏览器播放" };
        close.Clicked+=async (_,_)=>await navigation.PopModalAsync();
        reload.Clicked+=(_,_)=>player.Source=TeachingResources.PlayerUrl(video);
        browser.Clicked+=async (_,_)=>
        {
            try
            {
                // Only an explicit user tap launches the known official video URL.
                await Browser.Default.OpenAsync(TeachingResources.BrowserUrl(video),BrowserLaunchMode.External);
            }
            catch { status.Text="无法打开浏览器，请确认手机已安装浏览器后重试。"; }
        };
        var layout=new Grid { Padding=12,RowDefinitions={new(GridLength.Auto),new(GridLength.Auto),new(GridLength.Star),new(GridLength.Auto)} };
        layout.Add(new Label { Text=video.Title,FontSize=18 },0,0);
        layout.Add(status,0,1);
        layout.Add(player,0,2);
        layout.Add(new HorizontalStackLayout { Spacing=8,Children={close,reload,browser} },0,3);
        var page=new ContentPage { Title="课堂视频",Content=layout };
        // Unload the media on toolbar close, hardware back, or modal dismissal.
        page.Disappearing+=(_,_)=>player.Source=null;
        player.Source=TeachingResources.PlayerUrl(video);
        await navigation.PushModalAsync(page);
    });
}
