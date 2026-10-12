using MathPet.Core;
namespace MathPet.UI;
public interface IAppMaintenance
{
    string CurrentVersion { get; }
    Task<AppUpdate?> CheckAsync();
    Task OpenDownloadAsync(AppUpdate update);
    Task ExportAsync(string json);
    Task<string?> PickBackupAsync();
    Task PreserveBeforeImportAsync();
}
