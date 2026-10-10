using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;
public sealed class TeachingVideoTests
{
    [Fact]
    public void EveryMappedVideoUsesItsOwnBvidAndPartInsideOfficialPlayer()
    {
        var videos=Curriculum.Topics.SelectMany(t=>TeachingResources.ForTopic(t.Id).Videos).ToArray();
        Assert.NotEmpty(videos);
        foreach(var video in videos)
        {
            var url=new Uri(TeachingResources.PlayerUrl(video));
            Assert.True(TeachingResources.IsPlayerNavigation(url));
            Assert.Contains($"bvid={video.Bvid}&p={video.Page}&",url.Query);
            Assert.Contains("autoplay=0",url.Query);Assert.Contains("danmaku=0",url.Query);
        }
    }
    [Theory]
    [InlineData("https://player.bilibili.com/player.html?bvid=BV18Z4y1N72X&p=10",true)]
    [InlineData("https://www.bilibili.com/blackboard/webplayer/mbplayer.html?bvid=BV18Z4y1N72X&p=10",true)]
    [InlineData("bilibili://video/372139514?page=9",false)]
    [InlineData("intent://video/372139514",false)]
    [InlineData("https://www.bilibili.com/video/BV18Z4y1N72X/?p=10",false)]
    [InlineData("https://player.bilibili.com.evil.example/player.html",false)]
    [InlineData("http://player.bilibili.com/player.html",false)]
    [InlineData("https://player.bilibili.com:444/player.html",false)]
    public void NavigationAllowsPlayersAndRejectsAppLaunchesAndRegularPages(string url,bool allowed)
        =>Assert.Equal(allowed,TeachingResources.IsPlayerNavigation(new Uri(url)));
    [Theory]
    [InlineData("BV18Z4y1N72X",0)]
    [InlineData("BV18Z4y1N72X",-1)]
    [InlineData("BV18Z4y1N72X&bad=1",1)]
    public void InvalidVideoMetadataDoesNotBecomeAnIframeUrl(string bvid,int page)
        =>Assert.Throws<ArgumentException>(()=>TeachingResources.PlayerUrl(new("bad","unused",page,bvid)));
}
