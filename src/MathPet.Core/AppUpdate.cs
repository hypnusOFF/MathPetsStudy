using System.Text.Json;
namespace MathPet.Core;

public sealed record AppUpdate(int VersionCode, string VersionName, string ApkUrl, string Sha256)
{
    public const string Repository = "https://github.com/hypnusOFF/MathPetsStudy";
    public static AppUpdate Parse(string json)
    {
        var result=JsonSerializer.Deserialize<AppUpdate>(json,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})
            ?? throw new InvalidDataException("版本信息为空。");
        if(result.VersionCode<=0 || string.IsNullOrWhiteSpace(result.VersionName)
            || result.Sha256 is null || result.Sha256.Length!=64 || !result.Sha256.All(Uri.IsHexDigit)
            || !Uri.TryCreate(result.ApkUrl,UriKind.Absolute,out var uri)
            || uri.Scheme!="https" || uri.Host!="github.com" || uri.Port!=443 || uri.UserInfo!=""
            || !uri.AbsolutePath.StartsWith("/hypnusOFF/MathPetsStudy/releases/download/",StringComparison.Ordinal)
            || !uri.AbsolutePath.EndsWith(".apk",StringComparison.Ordinal) || uri.Query!="" || uri.Fragment!="")
            throw new InvalidDataException("版本信息或下载地址不合法。");
        return result;
    }
}
