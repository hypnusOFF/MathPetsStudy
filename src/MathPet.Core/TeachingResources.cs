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
}
