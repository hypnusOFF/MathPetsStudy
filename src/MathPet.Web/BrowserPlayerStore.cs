using MathPet.Core;
using Microsoft.JSInterop;
namespace MathPet.Web;
public sealed class BrowserPlayerStore(IJSRuntime js):IPlayerStore
{
    public async Task<PlayerState?> LoadAsync()
    {
        var json=await js.InvokeAsync<string?>("mathPet.load");
        return json is null?null:StateSerialization.Deserialize(json);
    }
    public async Task SaveAsync(PlayerState state)=>await js.InvokeVoidAsync("mathPet.save",StateSerialization.Serialize(state));
}
