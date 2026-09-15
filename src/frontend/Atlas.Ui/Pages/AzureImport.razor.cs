using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class AzureImport : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private IAzureDevOpsService _azureDevOpsService { get; set; } = null!;

        private AzureConnection? Connection { get; set; }
        private List<AzureUser> Users { get; set; } = [];
        private HashSet<string> SelectedUsers { get; set; } = new(StringComparer.Ordinal);
        private List<AzureImportWorkItem> ImportWorkItems { get; set; } = [];
        private HashSet<Guid> SelectedWorkItems { get; set; } = [];
        private string ProjectId { get; set; } = "";
        private string TeamMemberId { get; set; } = "";
        private bool Loading { get; set; }
        private bool ImportingUsers { get; set; }
        private bool ImportingProductOwners { get; set; }
        private bool LinkingWorkItems { get; set; }
        private string? Error { get; set; }
        private string? ProductOwnerWarning { get; set; }

        private string? SelectedProjectName =>
            Guid.TryParse(this.ProjectId, out Guid id)
                ? this._cache.Projects.FirstOrDefault(p => p.Id == id)?.Name
                : null;

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnCacheChangedAsync;
            await this._cache.EnsureHydratedAsync();
            await LoadAsync();
        }

        private async void OnCacheChangedAsync()
        {
            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        }

        private async Task LoadAsync()
        {
            this.Loading = true;
            this.Error = null;
            try
            {
                AzureConnection? conn = await this._azureDevOpsService.TryGetConnectionAsync();
                if (conn is null)
                {
                    this.Connection = null;
                    this.Error = "Set Project ID and Team ID in Settings before importing users.";
                    this.Users = [];
                    this.ImportWorkItems = (await SafeListWorkItemsAsync()).ToList();
                    return;
                }

                this.Connection = conn;
                if (string.IsNullOrWhiteSpace(conn.Organization)
                    || string.IsNullOrWhiteSpace(conn.ProjectId)
                    || string.IsNullOrWhiteSpace(conn.TeamId))
                {
                    this.Error = "Set Project ID and Team ID in Settings before importing users.";
                    this.Users = [];
                }
                else
                {
                    IReadOnlyList<AzureUser> list = await this._azureDevOpsService.ListUsersAsync(
                        conn.Organization!, conn.ProjectId!, conn.TeamId!);
                    IReadOnlyList<AzureUser> imported = await this._azureDevOpsService.ListImportedUsersAsync();
                    HashSet<string> importedSet = new(
                        imported
                            .Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant())
                            .Where(s => s.Length > 0),
                        StringComparer.Ordinal);
                    this.Users = list
                        .Where(u => !importedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                        .ToList();
                }

                this.ImportWorkItems = (await SafeListWorkItemsAsync()).ToList();
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
            finally
            {
                this.Loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task<IReadOnlyList<AzureImportWorkItem>> SafeListWorkItemsAsync()
        {
            try
            {
                return await this._azureDevOpsService.ListImportWorkItemsAsync();
            }
            catch
            {
                return Array.Empty<AzureImportWorkItem>();
            }
        }

        private void SelectAllUsers() =>
            this.SelectedUsers = this.Users
                .Select(u => u.UniqueName ?? "")
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

        private void ClearSelectedUsers() => this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);

        private void ToggleUser(string uniqueName, ChangeEventArgs e)
        {
            HashSet<string> next = new(this.SelectedUsers, StringComparer.Ordinal);
            if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                next.Add(uniqueName);
            }
            else
            {
                next.Remove(uniqueName);
            }

            this.SelectedUsers = next;
        }

        private void ToggleWorkItem(Guid id, ChangeEventArgs e)
        {
            HashSet<Guid> next = new(this.SelectedWorkItems);
            if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                next.Add(id);
            }
            else
            {
                next.Remove(id);
            }

            this.SelectedWorkItems = next;
        }

        private void OnProjectChange(ChangeEventArgs e) => this.ProjectId = e.Value?.ToString() ?? "";
        private void OnTeamMemberChange(ChangeEventArgs e) => this.TeamMemberId = e.Value?.ToString() ?? "";

        private List<AzureUser> SelectedUserList() =>
            this.Users
                .Where(u => u.UniqueName is not null && this.SelectedUsers.Contains(u.UniqueName))
                .ToList();

        private async Task OnImportUsers()
        {
            if (this.ImportingUsers || this.SelectedUsers.Count == 0)
            {
                return;
            }

            this.Error = null;
            this.ProductOwnerWarning = null;
            this.ImportingUsers = true;
            try
            {
                List<AzureUser> selected = this.SelectedUserList();
                await this._azureDevOpsService.ImportTeamAsync(selected);
                await this._cache.RefetchTeamAsync();
                HashSet<string> selectedSet = new(
                    selected.Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant()),
                    StringComparer.Ordinal);
                this.Users = this.Users
                    .Where(u => !selectedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                    .ToList();
                this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
            finally
            {
                this.ImportingUsers = false;
            }
        }

        private async Task OnImportProductOwners()
        {
            if (this.ImportingProductOwners || this.SelectedUsers.Count == 0)
            {
                return;
            }

            this.Error = null;
            this.ProductOwnerWarning = null;
            this.ImportingProductOwners = true;
            try
            {
                List<AzureUser> selected = this.SelectedUserList();
                ImportProductOwnersResult result = await this._azureDevOpsService.ImportProductOwnersAsync(selected);
                await this._cache.RefetchProductOwnersAsync();
                HashSet<string> selectedSet = new(
                    selected.Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant()),
                    StringComparer.Ordinal);
                this.Users = this.Users
                    .Where(u => !selectedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                    .ToList();
                this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<ReusedProductOwnerName> reused = result.ReusedProductOwnerNames;
                if (reused.Count > 0)
                {
                    var details = string.Join(", ", reused.Select(r => $"{r.DisplayName} ({r.AzureUniqueName})"));
                    this.ProductOwnerWarning =
                        $"{reused.Count} product owner{(reused.Count == 1 ? "" : "s")} reused existing names: {details}";
                }
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
            finally
            {
                this.ImportingProductOwners = false;
            }
        }

        private async Task OnLinkWorkItems()
        {
            if (this.LinkingWorkItems || string.IsNullOrEmpty(this.ProjectId) || this.SelectedWorkItems.Count == 0)
            {
                return;
            }

            if (!Guid.TryParse(this.ProjectId, out Guid projectId))
            {
                return;
            }

            this.Error = null;
            this.ProductOwnerWarning = null;
            this.LinkingWorkItems = true;
            try
            {
                Guid? teamMemberId = Guid.TryParse(this.TeamMemberId, out Guid mid) ? mid : null;
                await this._azureDevOpsService.LinkWorkItemsAsync(new LinkAzureWorkItemsRequest
                {
                    AzureWorkItemIds = this.SelectedWorkItems.ToList(),
                    ProjectId = projectId,
                    TeamMemberId = teamMemberId
                });
                await this._cache.RefetchTeamAsync();
                await this._cache.RefetchProjectsAsync();
                this.SelectedWorkItems = [];
                this.ImportWorkItems = (await this._azureDevOpsService.ListImportWorkItemsAsync()).ToList();
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
            finally
            {
                this.LinkingWorkItems = false;
            }
        }

        private void OnIgnoreSelectedWorkItems()
        {
            if (this.SelectedWorkItems.Count == 0)
            {
                return;
            }

            this.ImportWorkItems = this.ImportWorkItems
                .Where(item => !this.SelectedWorkItems.Contains(item.Id))
                .ToList();
            this.SelectedWorkItems = [];
        }

        public void Dispose() => this._cache.Changed -= OnCacheChangedAsync;
    }
}
