using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Settings : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] LocalSettings Local { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] SettingsService SettingsService { get; set; } = default!;
    [Inject] AzureDevOpsService AzureDevOpsService { get; set; } = default!;

    int _staleDays = 10;
    string _azureBaseUrl = "";
    bool _aiPanelOpen;
    bool _saving;
    string? _settingsError;

    bool _azureLoading = true;
    bool _azureLoaded;
    bool _azureSaving;
    string? _azureError;
    string? _azureSyncMessage;
    string _org = "";
    string _project = "";
    string _areaPath = "";
    string _teamName = "";
    string _projectId = "";
    string _teamId = "";
    string _enabledYesNo = "yes";

    AtlasApiDTOsAzureDevOpsAzureSyncStateDto? _syncState;
    bool _syncRunning;
    bool _syncStateLoading;

    bool SyncInProgress => _syncRunning || string.Equals(_syncState?.LastRunStatus, "Running", StringComparison.Ordinal);

    string LastCompletedLabel => _syncState?.LastCompletedAtUtc is not null
        ? DisplayLabels.FormatReadableDateTime(_syncState.LastCompletedAtUtc.Value.ToString("o"))
        : "Never";

    string? LastAttemptedLabel => _syncState?.LastAttemptedAtUtc is not null
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

    void OnChanged() => InvokeAsync(() => { SyncFromCache(); StateHasChanged(); });

    void SyncFromCache()
    {
        if (Cache.Settings is null) return;
        _staleDays = Cache.Settings.StaleDays;
        _azureBaseUrl = Cache.Settings.AzureDevOpsBaseUrl ?? "";
        _aiPanelOpen = Cache.Settings.DefaultAiPanelOpen;
    }

    void OnStaleDaysInput(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out int v)) return;
        _staleDays = Math.Clamp(v, 1, 365);
        if (Cache.Settings is not null)
        {
            Cache.PatchSettings(new Models.Settings
            {
                StaleDays = _staleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = Cache.Settings.DefaultAiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = Cache.Settings.AzureDevOpsBaseUrl
            });
        }
    }

    void OnAzureBaseUrlInput(ChangeEventArgs e)
    {
        _azureBaseUrl = e.Value?.ToString() ?? "";
        if (Cache.Settings is not null)
        {
            Cache.PatchSettings(new Models.Settings
            {
                StaleDays = Cache.Settings.StaleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = Cache.Settings.DefaultAiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = _azureBaseUrl
            });
        }
    }

    async Task OnAiPanelChange(ChangeEventArgs e)
    {
        _aiPanelOpen = e.Value?.ToString() == "on";
        await Local.SaveDefaultAiPanelOpenAsync(_aiPanelOpen);
        if (Cache.Settings is not null)
        {
            Cache.PatchSettings(new Models.Settings
            {
                StaleDays = Cache.Settings.StaleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = _aiPanelOpen,
                Theme = Cache.Settings.Theme,
                AzureDevOpsBaseUrl = Cache.Settings.AzureDevOpsBaseUrl
            });
        }
    }

    async Task SaveSettings()
    {
        if (Cache.Settings is null || _saving) return;
        _saving = true;
        _settingsError = null;
        try
        {
            await SettingsService.UpdateAsync(new AtlasApiDTOsSettingsUpdateSettingsRequest
            {
                StaleDays = _staleDays,
                DefaultAiManualOnly = Cache.Settings.DefaultAiManualOnly,
                Theme = ApiMappers.ToApiTheme(Cache.Settings.Theme),
                AzureDevOpsBaseUrl = string.IsNullOrWhiteSpace(_azureBaseUrl) ? null : _azureBaseUrl
            });
            await Cache.RefetchSettingsAsync();
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

    async Task LoadAzureAsync()
    {
        _azureLoading = true;
        try
        {
            AtlasApiDTOsAzureDevOpsAzureConnectionDto conn = await AzureDevOpsService.GetConnectionAsync();
            ApplyConn(conn);
        }
        catch (AtlasApiException ex) when (ex.StatusCode == 404)
        {
            // Unconfigured Azure — empty form is expected in CI smoke.
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

    async Task LoadSyncStateAsync()
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

    void ApplyConn(AtlasApiDTOsAzureDevOpsAzureConnectionDto conn)
    {
        _org = conn.Organization ?? "";
        _project = conn.Project ?? "";
        _areaPath = conn.AreaPath ?? "";
        _teamName = conn.TeamName ?? "";
        _projectId = conn.ProjectId ?? "";
        _teamId = conn.TeamId ?? "";
        _enabledYesNo = conn.IsEnabled == true ? "yes" : "no";
    }

    void OpenAzureImport() => Nav.NavigateTo("/settings/azure-import");

    async Task SaveAzureConnection()
    {
        if (_azureSaving) return;
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
            await AzureDevOpsService.UpdateConnectionAsync(new AtlasApiDTOsAzureDevOpsUpdateAzureConnectionRequest
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

    async Task SyncNow()
    {
        if (SyncInProgress) return;
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
            AtlasApiDTOsAzureDevOpsAzureSyncResultDto result = await AzureDevOpsService.RunSyncAsync();
            if (result.Succeeded != true)
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
                int count = result.ItemsUpserted ?? 0;
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

    static string SyncStatusClass(string? status) => status switch
    {
        "Failed" => "textBad",
        "Running" => "textWarn",
        "Succeeded" => "mutedSmall",
        _ => ""
    };

    public void Dispose() => Cache.Changed -= OnChanged;
}
