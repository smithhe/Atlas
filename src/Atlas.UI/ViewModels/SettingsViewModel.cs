using System.Collections.ObjectModel;

namespace Atlas.UI.ViewModels;

public sealed class SettingsViewModel : PageViewModel
{
    private int _staleThresholdDays = 10;
    private string _azureDevOpsBaseUrl = "https://dev.azure.com/your-org (placeholder)";
    private bool _aiManualOnly = true;

    public SettingsViewModel(AiPanelViewModel ai) : base(ai)
    {
        KeyboardShortcuts = new ObservableCollection<string>
        {
            "Ctrl+K — Command palette (placeholder)",
            "Ctrl+F — Search (placeholder)",
            "Ctrl+N — Quick Add (placeholder)",
        };
    }

    public int StaleThresholdDays
    {
        get => _staleThresholdDays;
        set => SetProperty(ref _staleThresholdDays, value);
    }

    public bool AiManualOnly
    {
        get => _aiManualOnly;
        set => SetProperty(ref _aiManualOnly, value);
    }

    public string ThemeLocked => "Dark (locked)";

    public string AzureDevOpsBaseUrl
    {
        get => _azureDevOpsBaseUrl;
        set => SetProperty(ref _azureDevOpsBaseUrl, value);
    }

    public ObservableCollection<string> KeyboardShortcuts { get; }
}


