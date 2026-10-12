using MathPet.Core;
using MathPet.UI;
using Xunit;
namespace MathPet.Core.Tests;
public class SaveImportTests
{
    private sealed class Store:IPlayerStore
    {
        public PlayerState? Saved;public bool Fail;public int Writes;
        public Task<PlayerState?> LoadAsync()=>Task.FromResult(Saved);
        public Task SaveAsync(PlayerState state){if(Fail)throw new IOException();Writes++;Saved=state;return Task.CompletedTask;}
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Version\":99}")]
    [InlineData("{\"Version\":4,\"Coins\":0}")]
    public async Task InvalidBackupNeverWritesOrChangesCurrentProgress(string json)
    {
        var store=new Store();var game=new GameState(store);var current=game.Engine;
        await Assert.ThrowsAnyAsync<Exception>(()=>game.ImportAsync(json));
        Assert.Same(current,game.Engine);Assert.Equal(0,store.Writes);
    }
    [Fact] public async Task FailedWriteKeepsCurrentProgress()
    {
        var store=new Store{Fail=true};var game=new GameState(store);var current=game.Engine;
        var incoming=new PlayerState{Coins=1234};
        await Assert.ThrowsAsync<IOException>(()=>game.ImportAsync(StateSerialization.Serialize(incoming)));
        Assert.Same(current,game.Engine);Assert.Null(store.Saved);
    }
    [Fact] public async Task SuccessfulImportReplacesProgressAfterSaving()
    {
        var store=new Store();var game=new GameState(store);
        var incoming=new PlayerState{Coins=1234,Food=7};
        await game.ImportAsync(StateSerialization.Serialize(incoming));
        Assert.Equal(1234,game.Engine.State.Coins);Assert.Equal(7,game.Engine.State.Food);
        Assert.Same(store.Saved,game.Engine.State);Assert.Null(game.StorageError);
    }
}
