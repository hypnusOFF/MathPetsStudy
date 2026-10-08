using MathPet.Core;
using MathPet.UI;
using MathPet.Web;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<IPlayerStore,BrowserPlayerStore>();
builder.Services.AddScoped<GameState>();
var app=builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
