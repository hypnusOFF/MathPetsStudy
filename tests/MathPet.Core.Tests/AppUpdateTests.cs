using System.Text.Json;
using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;
public class AppUpdateTests
{
    private static string Manifest(string url,int code=10001,string? hash=null)=>JsonSerializer.Serialize(new{
        versionCode=code,versionName="0.2.0",apkUrl=url,sha256=hash??new string('a',64)});
    [Fact] public void ReadsPublicReleaseManifest()
    {
        var update=AppUpdate.Parse(Manifest(AppUpdate.Repository+"/releases/download/v0.2.0/mathpet-0.2.0.apk"));
        Assert.Equal(10001,update.VersionCode);Assert.Equal("0.2.0",update.VersionName);
    }
    [Theory]
    [InlineData("http://github.com/hypnusOFF/MathPetsStudy/releases/download/v1/a.apk")]
    [InlineData("https://github.com.evil.example/hypnusOFF/MathPetsStudy/releases/download/v1/a.apk")]
    [InlineData("https://github.com/other/MathPetsStudy/releases/download/v1/a.apk")]
    [InlineData("https://github.com/hypnusOFF/MathPetsStudy/releases/download/../../other/a.apk")]
    [InlineData("https://github.com/hypnusOFF/MathPetsStudy/releases/download/v1/a.zip")]
    [InlineData("https://github.com/hypnusOFF/MathPetsStudy/releases/download/v1/a.apk?redirect=bad")]
    [InlineData("https://user@github.com/hypnusOFF/MathPetsStudy/releases/download/v1/a.apk")]
    [InlineData("https://github.com:444/hypnusOFF/MathPetsStudy/releases/download/v1/a.apk")]
    public void RejectsUnofficialOrUnexpectedDownload(string url)=>Assert.Throws<InvalidDataException>(()=>AppUpdate.Parse(Manifest(url)));
    [Fact] public void RejectsInvalidCodeAndChecksum()
    {
        var url=AppUpdate.Repository+"/releases/download/v1/a.apk";
        Assert.Throws<InvalidDataException>(()=>AppUpdate.Parse(Manifest(url,0)));
        Assert.Throws<InvalidDataException>(()=>AppUpdate.Parse(Manifest(url,10001,new string('z',64))));
        Assert.Throws<InvalidDataException>(()=>AppUpdate.Parse("null"));
    }
}
