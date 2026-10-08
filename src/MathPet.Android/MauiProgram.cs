using MathPet.Core;
using MathPet.UI;
namespace MathPet.Android;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder=MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<IPlayerStore,FilePlayerStore>();
        builder.Services.AddSingleton<GameState>();
        return builder.Build();
    }
}
