using MathPet.Core;
using Xunit;
namespace MathPet.Core.Tests;
public sealed class PersistenceTests:IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"mathpet-tests-"+Guid.NewGuid());
    [Fact] public async Task FreshStoreThenSaveReopenAndReplace()
    {
        var store=new JsonFilePlayerStore(directory);Assert.Null(await store.LoadAsync());
        var state=new PlayerState{PetName="小团",Coins=250,HasToy=true};await store.SaveAsync(state);
        var reopened=new JsonFilePlayerStore(directory);var restored=await reopened.LoadAsync();
        Assert.NotNull(restored);Assert.Equal("小团",restored.PetName);Assert.Equal(250,restored.Coins);Assert.True(restored.HasToy);
        restored.Coins=200;await reopened.SaveAsync(restored);Assert.Equal(200,(await store.LoadAsync())!.Coins);
        Assert.False(File.Exists(store.SavePath+".tmp"));
    }
    [Fact] public async Task BadFileIsPreservedAndBackedUpWithoutBeingOverwritten()
    {
        Directory.CreateDirectory(directory);var store=new JsonFilePlayerStore(directory);await File.WriteAllTextAsync(store.SavePath,"not-json");
        await Assert.ThrowsAnyAsync<Exception>(()=>store.LoadAsync());Assert.Equal("not-json",await File.ReadAllTextAsync(store.SavePath));
        var backup=Assert.Single(Directory.GetFiles(directory,"*.bak"));Assert.Equal("not-json",await File.ReadAllTextAsync(backup));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
