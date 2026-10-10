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
        const string instruction="点击播放器中的播放按钮开始观看。";
        var status=new Label { Text=instruction,FontSize=13 };
        bool Allowed(Uri uri)=>uri.OriginalString=="about:blank"||TeachingResources.IsPlayerNavigation(uri);
        player.HandlerChanged+=(_,_)=>
        {
            if(player.Handler?.PlatformView is global::Android.Webkit.WebView native)
            {
                native.Settings.JavaScriptEnabled=true;
                native.Settings.DomStorageEnabled=true;
                // Playback follows the child's explicit selection or play-button tap.
                native.Settings.MediaPlaybackRequiresUserGesture=false;
                var agent=native.Settings.UserAgentString??"";
                var chrome=System.Text.RegularExpressions.Regex.Match(agent,@"Chrome/[\d.]+").Value;
                native.Settings.UserAgentString=$"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) {chrome} Safari/537.36";
                if(player.Handler is Microsoft.Maui.Handlers.WebViewHandler handler)
                    native.SetWebViewClient(new LessonWebViewClient(handler,Allowed));
                var cookies=global::Android.Webkit.CookieManager.Instance;
                cookies?.SetAcceptCookie(true);
                cookies?.SetAcceptThirdPartyCookies(native,true);
            }
        };
        player.Navigating+=(_,e)=>
        {
            if(!Uri.TryCreate(e.Url,UriKind.Absolute,out var uri)
                || !Allowed(uri))
                e.Cancel=true;
        };
        player.Navigated+=(_,e)=>
        {
            if(e.Result!=WebNavigationResult.Success)
                status.Text="播放器加载失败，请检查网络后重新加载。";
        };
        var close=new Button { Text="返回课堂" };
        var reload=new Button { Text="重新加载" };
        close.Clicked+=async (_,_)=>await navigation.PopModalAsync();
        reload.Clicked+=(_,_)=>
        {
            status.Text=instruction;
            player.Source=TeachingResources.PlayerUrl(video);
        };
        var layout=new Grid { Padding=12,RowDefinitions={new(GridLength.Auto),new(GridLength.Auto),new(GridLength.Star),new(GridLength.Auto)} };
        layout.Add(new Label { Text=video.Title,FontSize=18 },0,0);
        layout.Add(status,0,1);
        layout.Add(player,0,2);
        layout.Add(new HorizontalStackLayout { Spacing=8,Children={close,reload} },0,3);
        var page=new ContentPage { Title="课堂视频",Content=layout };
        // Unload the media on toolbar close, hardware back, or modal dismissal.
        page.Disappearing+=(_,_)=>
        {
            if(player.Handler?.PlatformView is global::Android.Webkit.WebView native)
            {
                native.StopLoading();
                native.LoadUrl("about:blank");
            }
            player.Source=null;
        };
        player.Source=TeachingResources.PlayerUrl(video);
        await navigation.PushModalAsync(page);
    });
}

sealed class LessonWebViewClient(Microsoft.Maui.Handlers.WebViewHandler handler,Func<Uri,bool> allowed)
    :Microsoft.Maui.Platform.MauiWebViewClient(handler)
{
    private bool Block(string? url)=>!Uri.TryCreate(url,UriKind.Absolute,out var uri)||!allowed(uri);
    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view,global::Android.Webkit.IWebResourceRequest? request)
        =>Block(request?.Url?.ToString())||base.ShouldOverrideUrlLoading(view,request);
    [Obsolete("Android calls this overload on older WebView implementations.")]
    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view,string? url)=>Block(url);
}
