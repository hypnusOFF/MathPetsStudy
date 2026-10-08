using MathPet.Core;
namespace MathPet.Android;
public sealed class FilePlayerStore:IPlayerStore
{
    private readonly JsonFilePlayerStore store=new(FileSystem.AppDataDirectory);
    public Task<PlayerState?> LoadAsync()=>store.LoadAsync();
    public Task SaveAsync(PlayerState state)=>store.SaveAsync(state);
}
