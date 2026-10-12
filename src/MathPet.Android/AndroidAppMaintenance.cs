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
    public async Task ExportAsync(string json)
    {
        var path=Path.Combine(FileSystem.CacheDirectory,$"mathpet-backup-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path,json);
        await Share.Default.RequestAsync(new ShareFileRequest{Title="保存数学宠物岛存档",File=new ShareFile(path,"application/json")});
    }
    public async Task<string?> PickBackupAsync()
    {
        var file=await FilePicker.Default.PickAsync(new PickOptions{PickerTitle="选择数学宠物岛存档 JSON"});
        if(file is null)return null;
        await using var stream=await file.OpenReadAsync();
        // Bound input size even when a document provider does not expose its length.
        using var buffer=new MemoryStream();var chunk=new byte[8192];int count;
        while((count=await stream.ReadAsync(chunk))>0){if(buffer.Length+count>4*1024*1024)throw new InvalidDataException("存档文件过大。");await buffer.WriteAsync(chunk.AsMemory(0,count));}
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
    public Task PreserveBeforeImportAsync()
    {
        var path=Path.Combine(FileSystem.AppDataDirectory,"player-v1.json");
        if(File.Exists(path))File.Copy(path,path+$".before-import-{Guid.NewGuid():N}.bak");
        return Task.CompletedTask;
    }
}
