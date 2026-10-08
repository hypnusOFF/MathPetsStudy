namespace MathPet.Core;

/// <summary>Platform-independent file persistence used by the Android app.</summary>
public sealed class JsonFilePlayerStore(string directory):IPlayerStore
{
    public string SavePath=>Path.Combine(directory,"player-v1.json");
    public async Task<PlayerState?> LoadAsync()
    {
        if(!File.Exists(SavePath))return null;
        try{return StateSerialization.Deserialize(await File.ReadAllTextAsync(SavePath));}
        catch
        {
            // Preserve unknown versions and corrupt files for recovery; do not reset automatically.
            var backup=SavePath+$".unreadable-{Guid.NewGuid():N}.bak";
            File.Copy(SavePath,backup,overwrite:false);
            throw;
        }
    }
    public async Task SaveAsync(PlayerState state)
    {
        Directory.CreateDirectory(directory);
        var temp=SavePath+".tmp";
        await File.WriteAllTextAsync(temp,StateSerialization.Serialize(state));
        File.Move(temp,SavePath,overwrite:true);
    }
}
