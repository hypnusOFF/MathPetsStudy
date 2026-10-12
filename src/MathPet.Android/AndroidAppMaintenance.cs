using MathPet.Core;
using MathPet.UI;
namespace MathPet.Android;
public sealed class AndroidAppMaintenance : IAppMaintenance
{
    private static readonly HttpClient Client=new(){Timeout=TimeSpan.FromSeconds(20)};
    public string CurrentVersion=>AppInfo.Current.VersionString;
    public async Task<AppUpdate?> CheckAsync()
    {
        var json=await Client.GetStringAsync(AppUpdate.Repository+"/releases/latest/download/update.json");
        var update=AppUpdate.Parse(json);
        return int.TryParse(AppInfo.Current.BuildString,out var code)&&update.VersionCode>code?update:null;
    }
    public async Task OpenDownloadAsync(AppUpdate update)
    {
        // Revalidate before handing the official URL to the system browser.
        AppUpdate.Parse(System.Text.Json.JsonSerializer.Serialize(update));
        if(!await Launcher.Default.OpenAsync(new Uri(update.ApkUrl)))throw new IOException("无法打开下载地址。");
    }
}
