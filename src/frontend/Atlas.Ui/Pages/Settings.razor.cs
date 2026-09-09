using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using AtlasSettings = Atlas.Ui.Models.Settings;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Settings : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private LocalSettings Local { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private SettingsService SettingsService { get; set; } = null!;
    [Inject] private AzureDevOpsService AzureDevOpsService { get; set; } = null!;

    private int _staleDays = 10;
    private string _azureBaseUrl = "";
    private bool _aiPanelOpen;
    private bool _saving;
    private string? _settingsError;

    private bool _azureLoading = true;
    private bool _azureLoaded;
    private bool _azureSaving;
    private string? _azureError;
    private string? _azureSyncMessage;
    private string _org = "";
    private string _project = "";
    private string _areaPath = "";
    private string _teamName = "";
    private string _projectId = "";
    private string _teamId = "";
    private string _enabledYesNo = "yes";

    private AzureSyncState? _syncState;
    private bool _syncRunning;
    private bool _syncStateLoading;

    private bool SyncInProgress => _syncRunning || string.Equals(_syncState?.LastRunStatus, "Running", StringComparison.Ordinal);

    private string LastCompletedLabel => _syncState?.LastCompletedAtUtc is not null
        ? DisplayLabels.FormatReadableDateTime(_syncState.LastCompletedAtUtc.Value.ToString("o"))
        : "Never";

    private string? LastAttemptedLabel => _syncState?.LastAttemptedAtUtc is not null
        ? DisplayLabels.FormatReadableDateTime(_syncState.LastAttemptedAtUtc.Value.ToString("o"))
        : null;

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Ai.SetContext("Context: Settings", [new AiAction("settings-help", "Explain settings")]);
        _ = Cache.EnsureHydratedAsync();
        SyncFromCache();
        _ = LoadAzureAsync();
        _ = LoadSyncStateAsync();
    }

    private void OnChanged() => InvokeAsync(() => { SyncFromCache(); StateHasChanged(); });

    private void SyncFromCache()
    {
        if (Cache.Settings is null)
        {
            return;
        }

        _staleDays = Cache.Settings.StaleDays;
        _azureBaseUrl = Cache.Settings.AzureDevOpsBaseUrl ?? "";
        _aiPanelOpen = Cache.Settings.DefaultAiPanelOpen;
    }

    private void OnStaleDaysInput(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var v))
        {
            return;
        }

        _staleDays = Math.Clamp(v, 1, 365);
        if (Cache.Settings is not null)
        {
            SettingsService.PatchLocal(new AtlasSettings
            {
                StaleDays = _staleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = Cache.Settings.DefaultAiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = Cache.Settings.AzureDevOpsBaseUrl
            });
        }
    }

    private void OnAzureBaseUrlInput(ChangeEventArgs e)
    {
        _azureBaseUrl = e.Value?.ToString() ?? "";
        if (Cache.Settings is not null)
        {
            SettingsService.PatchLocal(new AtlasSettings
            {
                StaleDays = Cache.Settings.StaleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = Cache.Settings.DefaultAiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = _azureBaseUrl
            });
        }
    }

    private async Task OnAiPanelChange(ChangeEventArgs e)
    {
        _aiPanelOpen = e.Value?.ToString() == "on";
        await Local.SaveDefaultAiPanelOpenAsync(_aiPanelOpen);
        if (Cache.Settings is not null)
        {
            SettingsService.PatchLocal(new AtlasSettings
            {
                StaleDays = Cache.Settings.StaleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = _aiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = Cache.Settings.AzureDevOpsBaseUrl
            });
        }
    }

    private async Task SaveSettings()
    {
        if (Cache.Settings is null || _saving)
        {
            return;
        }

        _saving = true;
        _settingsError = null;
        try
        {
            await SettingsService.UpdateAsync(new AtlasSettings
            {
                StaleDays = _staleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = Cache.Settings.DefaultAiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = string.IsNullOrWhiteSpace(_azureBaseUrl) ? null : _azureBaseUrl
            });
            SyncFromCache();
        }
        catch (Exception ex)
        {
            _settingsError = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task LoadAzureAsync()
    {
        _azureLoading = true;
        try
        {
            AzureConnection? conn = await AzureDevOpsService.TryGetConnectionAsync();
            if (conn is null)
            {
                // Unconfigured Azure — empty form is expected in CI smoke.
                return;
            }

            ApplyConn(conn);
        }
        catch (Exception ex)
        {
            _azureError = ex.Message;
        }
        finally
        {
            _azureLoaded = true;
            _azureLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadSyncStateAsync()
    {
        _syncStateLoading = true;
        try
        {
            _syncState = await AzureDevOpsService.GetSyncStateAsync();
        }
        catch
        {
            _syncState = null;
        }
        finally
        {
            _syncStateLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ApplyConn(AzureConnection conn)
    {
        _org = conn.Organization ?? "";
        _project = conn.Project ?? "";
        _areaPath = conn.AreaPath ?? "";
        _teamName = conn.TeamName ?? "";
        _projectId = conn.ProjectId ?? "";
        _teamId = conn.TeamId ?? "";
        _enabledYesNo = conn.IsEnabled ? "yes" : "no";
    }

    private void OpenAzureImport() => Nav.NavigateTo("/settings/azure-import");

    private async Task SaveAzureConnection()
    {
        if (_azureSaving)
        {
            return;
        }

        _azureError = null;
        _azureSyncMessage = null;
        if (string.IsNullOrWhiteSpace(_projectId) || string.IsNullOrWhiteSpace(_teamId))
        {
            _azureError = "Project ID and Team ID are required. Use Azure Setup to select a project and team.";
            return;
        }

        _azureSaving = true;
        try
        {
            await AzureDevOpsService.UpdateConnectionAsync(new AzureUpdateConnection
            {
                Organization = _org,
                Project = _project,
                AreaPath = _areaPath,
                TeamName = string.IsNullOrWhiteSpace(_teamName) ? null : _teamName,
                ProjectId = _projectId,
                TeamId = _teamId,
                IsEnabled = _enabledYesNo == "yes"
            });
            _azureLoaded = true;
        }
        catch (Exception ex)
        {
            _azureError = ex.Message;
        }
        finally
        {
            _azureSaving = false;
        }
    }

    private async Task SyncNow()
    {
        if (SyncInProgress)
        {
            return;
        }

        _azureError = null;
        _azureSyncMessage = null;

        if (string.IsNullOrWhiteSpace(_org) || string.IsNullOrWhiteSpace(_projectId))
        {
            _azureError = "Azure sync needs Organization and Project ID first. Use Azure Setup or fill them in Settings.";
            return;
        }

        _syncRunning = true;
        try
        {
            AzureSyncResult result = await AzureDevOpsService.RunSyncAsync();
            if (!result.Succeeded)
            {
                _azureError = !string.IsNullOrWhiteSpace(result.Error)
                    ? result.Error
                    : "Azure sync failed. Check AzureDevopsToken (user-secrets, appsettings, or environment) and connection settings.";
            }
            else if (!string.IsNullOrWhiteSpace(result.Error))
            {
                _azureError = result.Error;
            }
            else
            {
                var count = result.ItemsUpserted ?? 0;
                _azureSyncMessage = count == 1
                    ? "Sync succeeded · 1 work item upserted"
                    : $"Sync succeeded · {count} work items upserted";
            }

            await Cache.RefetchTeamAsync();

            _syncStateLoading = true;
            StateHasChanged();
            try
            {
                _syncState = await AzureDevOpsService.GetSyncStateAsync();
            }
            catch
            {
                // Keep existing sync state on refetch failure.
            }
        }
        catch (Exception ex)
        {
            _azureError = ex.Message;
        }
        finally
        {
            _syncRunning = false;
            _syncStateLoading = false;
        }
    }

    private static string SyncStatusClass(string? status) => status switch
    {
        "Failed" => "textBad",
        "Running" => "textWarn",
        "Succeeded" => "mutedSmall",
        _ => ""
    };

    public void Dispose() => Cache.Changed -= OnChanged;
}
