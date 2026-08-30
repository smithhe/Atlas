using Microsoft.AspNetCore.Components;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class AzureImport : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private AzureDevOpsService AzureDevOpsService { get; set; } = null!;

    private AzureConnection? _connection;
    private List<AzureUser> _users = [];
    private HashSet<string> _selectedUsers = new(StringComparer.Ordinal);
    private List<AzureImportWorkItem> _importWorkItems = [];
    private HashSet<Guid> _selectedWorkItems = [];
    private string _projectId = "";
    private string _teamMemberId = "";
    private bool _loading;
    private bool _importingUsers;
    private bool _importingProductOwners;
    private bool _linkingWorkItems;
    private string? _error;
    private string? _productOwnerWarning;

    private string? _selectedProjectName =>
        Guid.TryParse(_projectId, out Guid id)
            ? Cache.Projects.FirstOrDefault(p => p.Id == id)?.Name
            : null;

    protected override void OnInitialized()
    {
        Cache.Changed += OnCacheChanged;
        _ = Cache.EnsureHydratedAsync();
        _ = LoadAsync();
    }

    private void OnCacheChanged() => InvokeAsync(StateHasChanged);

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            AzureConnection? conn = await AzureDevOpsService.TryGetConnectionAsync();
            if (conn is null)
            {
                _connection = null;
                _error = "Set Project ID and Team ID in Settings before importing users.";
                _users = [];
                _importWorkItems = (await SafeListWorkItemsAsync()).ToList();
                return;
            }

            _connection = conn;
            if (string.IsNullOrWhiteSpace(conn.Organization)
                || string.IsNullOrWhiteSpace(conn.ProjectId)
                || string.IsNullOrWhiteSpace(conn.TeamId))
            {
                _error = "Set Project ID and Team ID in Settings before importing users.";
                _users = [];
            }
            else
            {
                IReadOnlyList<AzureUser> list = await AzureDevOpsService.ListUsersAsync(
                    conn.Organization!, conn.ProjectId!, conn.TeamId!);
                IReadOnlyList<AzureUser> imported = await AzureDevOpsService.ListImportedUsersAsync();
                HashSet<string> importedSet = new(
                    imported
                        .Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant())
                        .Where(s => s.Length > 0),
                    StringComparer.Ordinal);
                _users = list
                    .Where(u => !importedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                    .ToList();
            }

            _importWorkItems = (await SafeListWorkItemsAsync()).ToList();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<IReadOnlyList<AzureImportWorkItem>> SafeListWorkItemsAsync()
    {
        try
        {
            return await AzureDevOpsService.ListImportWorkItemsAsync();
        }
        catch
        {
            return Array.Empty<AzureImportWorkItem>();
        }
    }

    private void SelectAllUsers() =>
        _selectedUsers = _users
            .Select(u => u.UniqueName ?? "")
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

    private void ClearSelectedUsers() => _selectedUsers = new HashSet<string>(StringComparer.Ordinal);

    private void ToggleUser(string uniqueName, ChangeEventArgs e)
    {
        HashSet<string> next = new(_selectedUsers, StringComparer.Ordinal);
        if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            next.Add(uniqueName);
        }
        else
        {
            next.Remove(uniqueName);
        }

        _selectedUsers = next;
    }

    private void ToggleWorkItem(Guid id, ChangeEventArgs e)
    {
        HashSet<Guid> next = new(_selectedWorkItems);
        if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            next.Add(id);
        }
        else
        {
            next.Remove(id);
        }

        _selectedWorkItems = next;
    }

    private void OnProjectChange(ChangeEventArgs e) => _projectId = e.Value?.ToString() ?? "";
    private void OnTeamMemberChange(ChangeEventArgs e) => _teamMemberId = e.Value?.ToString() ?? "";

    private List<AzureUser> SelectedUsers() =>
        _users
            .Where(u => u.UniqueName is not null && _selectedUsers.Contains(u.UniqueName))
            .ToList();

    private async Task OnImportUsers()
    {
        if (_importingUsers || _selectedUsers.Count == 0)
        {
            return;
        }

        _error = null;
        _productOwnerWarning = null;
        _importingUsers = true;
        try
        {
            List<AzureUser> selected = SelectedUsers();
            await AzureDevOpsService.ImportTeamAsync(selected);
            await Cache.RefetchTeamAsync();
            HashSet<string> selectedSet = new(
                selected.Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant()),
                StringComparer.Ordinal);
            _users = _users
                .Where(u => !selectedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                .ToList();
            _selectedUsers = new HashSet<string>(StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _importingUsers = false;
        }
    }

    private async Task OnImportProductOwners()
    {
        if (_importingProductOwners || _selectedUsers.Count == 0)
        {
            return;
        }

        _error = null;
        _productOwnerWarning = null;
        _importingProductOwners = true;
        try
        {
            List<AzureUser> selected = SelectedUsers();
            ImportProductOwnersResult result = await AzureDevOpsService.ImportProductOwnersAsync(selected);
            await Cache.RefetchProductOwnersAsync();
            HashSet<string> selectedSet = new(
                selected.Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant()),
                StringComparer.Ordinal);
            _users = _users
                .Where(u => !selectedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                .ToList();
            _selectedUsers = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<ReusedProductOwnerName> reused = result.ReusedProductOwnerNames;
            if (reused.Count > 0)
            {
                var details = string.Join(", ", reused.Select(r => $"{r.DisplayName} ({r.AzureUniqueName})"));
                _productOwnerWarning =
                    $"{reused.Count} product owner{(reused.Count == 1 ? "" : "s")} reused existing names: {details}";
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _importingProductOwners = false;
        }
    }

    private async Task OnLinkWorkItems()
    {
        if (_linkingWorkItems || string.IsNullOrEmpty(_projectId) || _selectedWorkItems.Count == 0)
        {
            return;
        }

        if (!Guid.TryParse(_projectId, out Guid projectId))
        {
            return;
        }

        _error = null;
        _productOwnerWarning = null;
        _linkingWorkItems = true;
        try
        {
            Guid? teamMemberId = Guid.TryParse(_teamMemberId, out Guid mid) ? mid : null;
            await AzureDevOpsService.LinkWorkItemsAsync(new LinkAzureWorkItemsRequest
            {
                AzureWorkItemIds = _selectedWorkItems.ToList(),
                ProjectId = projectId,
                TeamMemberId = teamMemberId
            });
            await Cache.RefetchTeamAsync();
            await Cache.RefetchProjectsAsync();
            _selectedWorkItems = [];
            _importWorkItems = (await AzureDevOpsService.ListImportWorkItemsAsync()).ToList();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _linkingWorkItems = false;
        }
    }

    private void OnIgnoreSelectedWorkItems()
    {
        if (_selectedWorkItems.Count == 0)
        {
            return;
        }

        _importWorkItems = _importWorkItems
            .Where(item => !_selectedWorkItems.Contains(item.Id))
            .ToList();
        _selectedWorkItems = [];
    }

    public void Dispose() => Cache.Changed -= OnCacheChanged;
}
