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
        var status=new Label { Text="点击播放开始观看；若无响应，试试原网页播放或查看播放诊断。",FontSize=13 };
        var original=new Uri(TeachingResources.BrowserUrl(video));
        bool Allowed(Uri uri)=>uri.OriginalString=="about:blank"||TeachingResources.IsPlayerNavigation(uri)
            ||uri.Scheme=="https"&&uri.IsDefaultPort&&uri.Host==original.Host&&uri.AbsolutePath==original.AbsolutePath;
        string? mobileAgent=null,desktopAgent=null;
        var desktop=true;
        var lastError="暂未观察到网络错误";
        var networkErrors=new List<string>();
        var diagnostics=new Label { FontSize=12,IsVisible=false };
        void Load(string url)
        {
            networkErrors.Clear();lastError="暂未观察到网络错误";
            diagnostics.IsVisible=false;
            status.Text="点击播放开始观看；若无响应，试试原网页播放或查看播放诊断。";
            player.Source=url;
        }
        player.HandlerChanged+=(_,_)=>
        {
            if(player.Handler?.PlatformView is global::Android.Webkit.WebView native)
            {
                native.Settings.JavaScriptEnabled=true;
                native.Settings.DomStorageEnabled=true;
                // Playback follows the child's explicit selection or play-button tap.
                native.Settings.MediaPlaybackRequiresUserGesture=false;
                mobileAgent=native.Settings.UserAgentString;
                var chrome=System.Text.RegularExpressions.Regex.Match(mobileAgent??"",@"Chrome/[\d.]+").Value;
                desktopAgent=$"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) {chrome} Safari/537.36";
                native.Settings.UserAgentString=desktop?desktopAgent:mobileAgent;
                if(player.Handler is Microsoft.Maui.Handlers.WebViewHandler handler)
                    native.SetWebViewClient(new LessonWebViewClient(handler,Allowed,message=>
                    {
                        if(!networkErrors.Contains(message)&&networkErrors.Count<6)networkErrors.Add(message);
                        lastError=string.Join("\n",networkErrors);
                        MainThread.BeginInvokeOnMainThread(()=>status.Text=message+"。可尝试原网页播放。");
                    }));
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
                status.Text="播放器加载失败，请重新加载或使用浏览器播放。";
        };
        var close=new Button { Text="返回课堂" };
        var reload=new Button { Text="重新加载" };
        var browser=new Button { Text="用浏览器播放" };
        var originalPage=new Button { Text="原网页播放" };
        var mode=new Button { Text="切换手机版" };
        var diagnose=new Button { Text="查看播放诊断" };
        close.Clicked+=async (_,_)=>await navigation.PopModalAsync();
        reload.Clicked+=(_,_)=>Load(TeachingResources.PlayerUrl(video));
        originalPage.Clicked+=(_,_)=>Load(original.AbsoluteUri);
        mode.Clicked+=(_,_)=>
        {
            desktop=!desktop;
            mode.Text=desktop?"切换手机版":"切换电脑版";
            if(player.Handler?.PlatformView is global::Android.Webkit.WebView native)
                native.Settings.UserAgentString=desktop?desktopAgent:mobileAgent;
            Load(TeachingResources.PlayerUrl(video));
        };
        diagnose.Clicked+=async (_,_)=>
        {
            diagnostics.IsVisible=true;
            try
            {
                var media=await player.EvaluateJavaScriptAsync("JSON.stringify(Array.from(document.querySelectorAll('video')).map(v=>({duration:Number.isFinite(v.duration)?v.duration:null,readyState:v.readyState,networkState:v.networkState,paused:v.paused,error:v.error?v.error.code:null})))");
                var webview=OperatingSystem.IsAndroidVersionAtLeast(26)?global::Android.Webkit.WebView.CurrentWebViewPackage?.VersionName:"未知";
                var androidVersion=global::Android.OS.Build.VERSION.Release;
                var model=global::Android.OS.Build.Model;
                diagnostics.Text=$"Android {androidVersion} / {model}\nWebView {webview} / {(desktop?"电脑版":"手机版")}\n{lastError}\n媒体状态：{media}";
            }
            catch { diagnostics.Text=lastError+"；未能读取媒体状态，请重新加载后重试。"; }
        };
        browser.Clicked+=async (_,_)=>
        {
            try
            {
                // Only an explicit user tap launches the known official video URL.
                await Browser.Default.OpenAsync(TeachingResources.BrowserUrl(video),BrowserLaunchMode.External);
            }
            catch { status.Text="无法打开浏览器，请确认手机已安装浏览器后重试。"; }
        };
        var layout=new Grid { Padding=12,RowDefinitions={new(GridLength.Auto),new(GridLength.Auto),new(GridLength.Star),new(GridLength.Auto),new(GridLength.Auto)} };
        layout.Add(new Label { Text=video.Title,FontSize=18 },0,0);
        layout.Add(status,0,1);
        layout.Add(player,0,2);
        layout.Add(new FlexLayout { Wrap=Microsoft.Maui.Layouts.FlexWrap.Wrap,Children={close,reload,originalPage,mode,browser,diagnose} },0,3);
        layout.Add(diagnostics,0,4);
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

sealed class LessonWebViewClient(Microsoft.Maui.Handlers.WebViewHandler handler,Func<Uri,bool> allowed,Action<string> report)
    :Microsoft.Maui.Platform.MauiWebViewClient(handler)
{
    private bool Block(string? url)=>!Uri.TryCreate(url,UriKind.Absolute,out var uri)||!allowed(uri);
    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view,global::Android.Webkit.IWebResourceRequest? request)
        =>Block(request?.Url?.ToString())||base.ShouldOverrideUrlLoading(view,request);
    [Obsolete("Android calls this overload on older WebView implementations.")]
    public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view,string? url)=>Block(url);
    public override void OnReceivedHttpError(global::Android.Webkit.WebView? view,global::Android.Webkit.IWebResourceRequest? request,global::Android.Webkit.WebResourceResponse? response)
    {
        if(response is not null)report($"资源请求失败：HTTP {response.StatusCode}（{request?.Url?.Host}）");
        base.OnReceivedHttpError(view,request,response);
    }
    public override void OnReceivedError(global::Android.Webkit.WebView? view,global::Android.Webkit.IWebResourceRequest? request,global::Android.Webkit.WebResourceError? error)
    {
        report($"资源连接失败：{error?.ErrorCode}（{request?.Url?.Host}）");
        base.OnReceivedError(view,request,error);
    }
    public override void OnReceivedSslError(global::Android.Webkit.WebView? view,global::Android.Webkit.SslErrorHandler? sslHandler,global::Android.Net.Http.SslError? error)
    {
        report("资源 TLS 证书验证失败");
        // Keep certificate verification: never proceed past an SSL error.
        base.OnReceivedSslError(view,sslHandler,error);
    }
}
