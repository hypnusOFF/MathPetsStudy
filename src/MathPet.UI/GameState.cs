using MathPet.Core;
namespace MathPet.UI;

public sealed class GameState(IPlayerStore store)
{
    public GameEngine Engine { get; private set; } = new(new PlayerState());
    public string? StorageError { get; private set; }
    private readonly SemaphoreSlim gate = new(1,1);
    public async Task LoadAsync()
    {
        try { Engine = new(await store.LoadAsync() ?? new PlayerState()); Engine.ApplyAbsence(); }
        catch (Exception) { StorageError = "无法读取本地存档。原数据已保留；为避免覆盖，暂时使用临时进度。请重启应用或先备份存档。"; }
    }
    public async Task SaveAsync()
    {
        if (StorageError is not null) return;
        await gate.WaitAsync();
        try { Engine.State.LastSeen = DateTimeOffset.UtcNow; await store.SaveAsync(Engine.State); }
        catch (Exception) { StorageError = "本次进度未能保存。请检查设备可用空间，并重启应用。当前会话可以继续。"; }
        finally { gate.Release(); }
    }
    public async Task ResetAsync()
    {
        var next = new PlayerState();
        await gate.WaitAsync();
        try { await store.SaveAsync(next); Engine = new(next); StorageError = null; }
        finally { gate.Release(); }
    }
}
