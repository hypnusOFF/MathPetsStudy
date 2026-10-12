using MathPet.Core;
namespace MathPet.UI;
public interface IAppMaintenance
{
    string CurrentVersion { get; }
    Task<AppUpdate?> CheckAsync();
    Task OpenDownloadAsync(AppUpdate update);
}
