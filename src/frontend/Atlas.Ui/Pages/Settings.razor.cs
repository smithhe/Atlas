using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using AtlasSettings = Atlas.Ui.Models.Settings;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
public partial class Settings : IDisposable
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private LocalSettings _local { get; set; } = null!;
    [Inject] private IAiStateService _ai { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;
    [Inject] private ISettingsService _settingsService { get; set; } = null!;
    [Inject] private IAzureDevOpsService _azureDevOpsService { get; set; } = null!;

    private int StaleDays { get; set; } = 10;
    private string AzureBaseUrl { get; set; } = "";
    private bool AiPanelOpen { get; set; }
    private bool Saving { get; set; }
    private string? SettingsError { get; set; }

    private bool AzureLoading { get; set; } = true;
    private bool AzureLoaded { get; set; }
    private bool AzureSaving { get; set; }
    private string? AzureError { get; set; }
    private string? AzureSyncMessage { get; set; }
    private string Org { get; set; } = "";
    private string Project { get; set; } = "";
    private string AreaPath { get; set; } = "";
    private string TeamName { get; set; } = "";
    private string ProjectId { get; set; } = "";
    private string TeamId { get; set; } = "";
    private string EnabledYesNo { get; set; } = "yes";

    private AzureSyncState? SyncState { get; set; }
    private bool SyncRunning { get; set; }
    private bool SyncStateLoading { get; set; }

    private bool SyncInProgress => this.SyncRunning || string.Equals(this.SyncState?.LastRunStatus, "Running", StringComparison.Ordinal);

    private string LastCompletedLabel => this.SyncState?.LastCompletedAtUtc is not null
        ? DisplayLabels.FormatReadableDateTime(this.SyncState.LastCompletedAtUtc.Value.ToString("o"))
        : "Never";

    private string? LastAttemptedLabel => this.SyncState?.LastAttemptedAtUtc is not null
        ? DisplayLabels.FormatReadableDateTime(this.SyncState.LastAttemptedAtUtc.Value.ToString("o"))
        : null;

    protected override async Task OnInitializedAsync()
    {
        this._cache.Changed += OnChangedAsync;
        this._ai.SetContext("Context: Settings", [new AiAction("settings-help", "Explain settings")]);
        await this._cache.EnsureHydratedAsync();
        SyncFromCache();
        await LoadAzureAsync();
        await LoadSyncStateAsync();
    }

    private async void OnChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                SyncFromCache();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void SyncFromCache()
    {
        if (this._cache.Settings is null)
        {
            return;
        }

        this.StaleDays = this._cache.Settings.StaleDays;
        this.AzureBaseUrl = this._cache.Settings.AzureDevOpsBaseUrl ?? "";
        this.AiPanelOpen = this._cache.Settings.DefaultAiPanelOpen;
    }

    private void OnStaleDaysInput(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var v))
        {
            return;
        }

        this.StaleDays = Math.Clamp(v, 1, 365);
        if (this._cache.Settings is not null)
        {
            this._settingsService.PatchLocal(new AtlasSettings
            {
                StaleDays = this.StaleDays,
                DefaultAiManualOnly = this._cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = this._cache.Settings.DefaultAiPanelOpen,
                Theme = this._cache.Settings.Theme,
                AzureDevOpsBaseUrl = this._cache.Settings.AzureDevOpsBaseUrl
            });
        }
    }

    private void OnAzureBaseUrlInput(ChangeEventArgs e)
    {
        this.AzureBaseUrl = e.Value?.ToString() ?? "";
        if (this._cache.Settings is not null)
        {
            this._settingsService.PatchLocal(new AtlasSettings
            {
                StaleDays = this._cache.Settings.StaleDays,
                DefaultAiManualOnly = this._cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = this._cache.Settings.DefaultAiPanelOpen,
                Theme = this._cache.Settings.Theme,
                AzureDevOpsBaseUrl = this.AzureBaseUrl
            });
        }
    }

    private async Task OnAiPanelChange(ChangeEventArgs e)
    {
        this.AiPanelOpen = e.Value?.ToString() == "on";
        await this._local.SaveDefaultAiPanelOpenAsync(this.AiPanelOpen);
        if (this._cache.Settings is not null)
        {
            this._settingsService.PatchLocal(new AtlasSettings
            {
                StaleDays = this._cache.Settings.StaleDays,
                DefaultAiManualOnly = this._cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = this.AiPanelOpen,
                Theme = this._cache.Settings.Theme,
                AzureDevOpsBaseUrl = this._cache.Settings.AzureDevOpsBaseUrl
            });
        }
    }

    private async Task SaveSettings()
    {
        if (this._cache.Settings is null || this.Saving)
        {
            return;
        }

        this.Saving = true;
        this.SettingsError = null;
        try
        {
            await this._settingsService.UpdateAsync(new AtlasSettings
            {
                StaleDays = this.StaleDays,
                DefaultAiManualOnly = this._cache.Settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = this._cache.Settings.DefaultAiPanelOpen,
                Theme = this._cache.Settings.Theme,
                AzureDevOpsBaseUrl = string.IsNullOrWhiteSpace(this.AzureBaseUrl) ? null : this.AzureBaseUrl
            });
            SyncFromCache();
        }
        catch (Exception ex)
        {
            this.SettingsError = ex.Message;
        }
        finally
        {
            this.Saving = false;
        }
    }

    private async Task LoadAzureAsync()
    {
        this.AzureLoading = true;
        try
        {
            AzureConnection? conn = await this._azureDevOpsService.TryGetConnectionAsync();
            if (conn is null)
            {
                // Unconfigured Azure — empty form is expected in CI smoke.
                return;
            }

            ApplyConn(conn);
        }
        catch (Exception ex)
        {
            this.AzureError = ex.Message;
        }
        finally
        {
            this.AzureLoaded = true;
            this.AzureLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadSyncStateAsync()
    {
        this.SyncStateLoading = true;
        try
        {
            this.SyncState = await this._azureDevOpsService.GetSyncStateAsync();
        }
        catch
        {
            this.SyncState = null;
        }
        finally
        {
            this.SyncStateLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ApplyConn(AzureConnection conn)
    {
        this.Org = conn.Organization ?? "";
        this.Project = conn.Project ?? "";
        this.AreaPath = conn.AreaPath ?? "";
        this.TeamName = conn.TeamName ?? "";
        this.ProjectId = conn.ProjectId ?? "";
        this.TeamId = conn.TeamId ?? "";
        this.EnabledYesNo = conn.IsEnabled ? "yes" : "no";
    }

    private void OpenAzureImport() => this._nav.NavigateTo("/settings/azure-import");

    private async Task SaveAzureConnection()
    {
        if (this.AzureSaving)
        {
            return;
        }

        this.AzureError = null;
        this.AzureSyncMessage = null;
        if (string.IsNullOrWhiteSpace(this.ProjectId) || string.IsNullOrWhiteSpace(this.TeamId))
        {
            this.AzureError = "Project ID and Team ID are required. Use Azure Setup to select a project and team.";
            return;
        }

        this.AzureSaving = true;
        try
        {
            await this._azureDevOpsService.UpdateConnectionAsync(new AzureUpdateConnection
            {
                Organization = this.Org,
                Project = this.Project,
                AreaPath = this.AreaPath,
                TeamName = string.IsNullOrWhiteSpace(this.TeamName) ? null : this.TeamName,
                ProjectId = this.ProjectId,
                TeamId = this.TeamId,
                IsEnabled = this.EnabledYesNo == "yes"
            });
            this.AzureLoaded = true;
        }
        catch (Exception ex)
        {
            this.AzureError = ex.Message;
        }
        finally
        {
            this.AzureSaving = false;
        }
    }

    private async Task SyncNow()
    {
        if (SyncInProgress)
        {
            return;
        }

        this.AzureError = null;
        this.AzureSyncMessage = null;

        if (string.IsNullOrWhiteSpace(this.Org) || string.IsNullOrWhiteSpace(this.ProjectId))
        {
            this.AzureError = "Azure sync needs Organization and Project ID first. Use Azure Setup or fill them in Settings.";
            return;
        }

        this.SyncRunning = true;
        try
        {
            AzureSyncResult result = await this._azureDevOpsService.RunSyncAsync();
            if (!result.Succeeded)
            {
                this.AzureError = !string.IsNullOrWhiteSpace(result.Error)
                    ? result.Error
                    : "Azure sync failed. Check AzureDevopsToken (user-secrets, appsettings, or environment) and connection settings.";
            }
            else if (!string.IsNullOrWhiteSpace(result.Error))
            {
                this.AzureError = result.Error;
            }
            else
            {
                var count = result.ItemsUpserted ?? 0;
                this.AzureSyncMessage = count == 1
                    ? "Sync succeeded · 1 work item upserted"
                    : $"Sync succeeded · {count} work items upserted";
            }

            await this._cache.RefetchTeamAsync();

            this.SyncStateLoading = true;
            StateHasChanged();
            try
            {
                this.SyncState = await this._azureDevOpsService.GetSyncStateAsync();
            }
            catch
            {
                // Keep existing sync state on refetch failure.
            }
        }
        catch (Exception ex)
        {
            this.AzureError = ex.Message;
        }
        finally
        {
            this.SyncRunning = false;
            this.SyncStateLoading = false;
        }
    }

    private static string SyncStatusClass(string? status) => status switch
    {
        "Failed" => "textBad",
        "Running" => "textWarn",
        "Succeeded" => "mutedSmall",
        _ => ""
    };

    public void Dispose() => this._cache.Changed -= OnChangedAsync;
}
}
