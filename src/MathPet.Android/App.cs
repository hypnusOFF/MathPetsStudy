namespace MathPet.Android;
public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)=>new(new MainPage()) { Title="数学宠物岛" };
}
