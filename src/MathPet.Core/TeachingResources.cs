using System.Text.Json;
namespace MathPet.Core;
public sealed record CourseVideo(string Title,string Url,int Page,string Bvid);
public sealed record TopicVideos(string TopicId,CourseVideo[] Videos,string Status);
public static class TeachingResources
{
    private static readonly IReadOnlyDictionary<string,TopicVideos> Catalog=Load();
    private static IReadOnlyDictionary<string,TopicVideos> Load()
    {
        using var stream=typeof(TeachingResources).Assembly.GetManifestResourceStream("MathPet.Core.Content.course-videos.json")!;
        return JsonSerializer.Deserialize<TopicVideos[]>(stream)!.ToDictionary(t=>t.TopicId);
    }
    public static TopicVideos ForTopic(string id)=>Catalog.GetValueOrDefault(id)??new(id,[],"未找到明确标题匹配");
    public static string PlayerUrl(CourseVideo video)
    {
        if(video.Bvid.Length!=12||!video.Bvid.StartsWith("BV",StringComparison.Ordinal)
            ||video.Bvid.Any(c=>!char.IsAsciiLetterOrDigit(c))||video.Page<1)
            throw new ArgumentException("视频分集信息不正确。");
        return $"https://player.bilibili.com/player.html?bvid={video.Bvid}&p={video.Page.ToString(System.Globalization.CultureInfo.InvariantCulture)}&autoplay=0&danmaku=0";
    }
    // Only official embedded players may navigate inside the app's WebView.
    public static bool IsPlayerNavigation(Uri url)=>url.Scheme=="https"&&url.IsDefaultPort&&
        (url.Host=="player.bilibili.com"&&url.AbsolutePath=="/player.html"
        ||url.Host=="www.bilibili.com"&&url.AbsolutePath=="/blackboard/webplayer/mbplayer.html");
}
