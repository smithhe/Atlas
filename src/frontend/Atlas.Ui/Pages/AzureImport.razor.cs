using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class AzureImport : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] AzureDevOpsService AzureDevOpsService { get; set; } = default!;

    AtlasApiDTOsAzureDevOpsAzureConnectionDto? _connection;
    List<AtlasApiDTOsAzureDevOpsAzureUserDto> _users = [];
    HashSet<string> _selectedUsers = new(StringComparer.Ordinal);
    List<AtlasApiDTOsAzureDevOpsAzureImportWorkItemDto> _importWorkItems = [];
    HashSet<Guid> _selectedWorkItems = [];
    string _projectId = "";
    string _teamMemberId = "";
    bool _loading;
    bool _importingUsers;
    bool _importingProductOwners;
    bool _linkingWorkItems;
    string? _error;
    string? _productOwnerWarning;

    string? _selectedProjectName =>
        Guid.TryParse(_projectId, out Guid id)
            ? Cache.Projects.FirstOrDefault(p => p.Id == id)?.Name
            : null;

    protected override void OnInitialized()
    {
        Cache.Changed += OnCacheChanged;
        _ = Cache.EnsureHydratedAsync();
        _ = LoadAsync();
    }

    void OnCacheChanged() => InvokeAsync(StateHasChanged);

    async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            AtlasApiDTOsAzureDevOpsAzureConnectionDto? conn;
            try
            {
                conn = await AzureDevOpsService.GetConnectionAsync();
            }
            catch (AtlasApiException ex) when (ex.StatusCode == 404)
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
                ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto> list = await AzureDevOpsService.ListUsersAsync(
                    conn.Organization!, conn.ProjectId!, conn.TeamId!);
                ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto> imported = await AzureDevOpsService.ListImportedUsersAsync();
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

    async Task<ICollection<AtlasApiDTOsAzureDevOpsAzureImportWorkItemDto>> SafeListWorkItemsAsync()
    {
        try
        {
            return await AzureDevOpsService.ListImportWorkItemsAsync();
        }
        catch
        {
            return Array.Empty<AtlasApiDTOsAzureDevOpsAzureImportWorkItemDto>();
        }
    }

    void SelectAllUsers() =>
        _selectedUsers = _users
            .Select(u => u.UniqueName ?? "")
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

    void ClearSelectedUsers() => _selectedUsers = new HashSet<string>(StringComparer.Ordinal);

    void ToggleUser(string uniqueName, ChangeEventArgs e)
    {
        HashSet<string> next = new(_selectedUsers, StringComparer.Ordinal);
        if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            next.Add(uniqueName);
        else
            next.Remove(uniqueName);
        _selectedUsers = next;
    }

    void ToggleWorkItem(Guid id, ChangeEventArgs e)
    {
        HashSet<Guid> next = new(_selectedWorkItems);
        if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            next.Add(id);
        else
            next.Remove(id);
        _selectedWorkItems = next;
    }

    void OnProjectChange(ChangeEventArgs e) => _projectId = e.Value?.ToString() ?? "";
    void OnTeamMemberChange(ChangeEventArgs e) => _teamMemberId = e.Value?.ToString() ?? "";

    List<AtlasApiDTOsAzureDevOpsAzureUserSelectionDto> SelectedUserDtos() =>
        _users
            .Where(u => u.UniqueName is not null && _selectedUsers.Contains(u.UniqueName))
            .Select(u => new AtlasApiDTOsAzureDevOpsAzureUserSelectionDto
            {
                DisplayName = u.DisplayName,
                UniqueName = u.UniqueName,
                Descriptor = u.Descriptor
            })
            .ToList();

    async Task OnImportUsers()
    {
        if (_importingUsers || _selectedUsers.Count == 0) return;
        _error = null;
        _productOwnerWarning = null;
        _importingUsers = true;
        try
        {
            List<AtlasApiDTOsAzureDevOpsAzureUserSelectionDto> selected = SelectedUserDtos();
            await AzureDevOpsService.ImportTeamAsync(
                new AtlasApiDTOsAzureDevOpsImportAzureTeamRequest { Users = selected });
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

    async Task OnImportProductOwners()
    {
        if (_importingProductOwners || _selectedUsers.Count == 0) return;
        _error = null;
        _productOwnerWarning = null;
        _importingProductOwners = true;
        try
        {
            List<AtlasApiDTOsAzureDevOpsAzureUserSelectionDto> selected = SelectedUserDtos();
            AtlasApiDTOsAzureDevOpsImportAzureProductOwnersResultDto result = await AzureDevOpsService.ImportProductOwnersAsync(
                new AtlasApiDTOsAzureDevOpsImportAzureProductOwnersRequest { Users = selected });
            await Cache.RefetchProductOwnersAsync();
            HashSet<string> selectedSet = new(
                selected.Select(u => (u.UniqueName ?? "").Trim().ToLowerInvariant()),
                StringComparer.Ordinal);
            _users = _users
                .Where(u => !selectedSet.Contains((u.UniqueName ?? "").Trim().ToLowerInvariant()))
                .ToList();
            _selectedUsers = new HashSet<string>(StringComparer.Ordinal);
            List<AtlasApiDTOsAzureDevOpsReusedProductOwnerNameDto> reused = result.ReusedProductOwnerNames?.ToList() ?? [];
            if (reused.Count > 0)
            {
                string details = string.Join(", ", reused.Select(r => $"{r.DisplayName} ({r.AzureUniqueName})"));
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

    async Task OnLinkWorkItems()
    {
        if (_linkingWorkItems || string.IsNullOrEmpty(_projectId) || _selectedWorkItems.Count == 0) return;
        if (!Guid.TryParse(_projectId, out Guid projectId)) return;
        _error = null;
        _productOwnerWarning = null;
        _linkingWorkItems = true;
        try
        {
            Guid? teamMemberId = Guid.TryParse(_teamMemberId, out Guid mid) ? mid : null;
            await AzureDevOpsService.LinkWorkItemsAsync(
                new AtlasApiDTOsAzureDevOpsLinkAzureWorkItemsRequest
                {
                    AzureWorkItemIds = _selectedWorkItems.ToList(),
                    ProjectId = projectId,
                    TeamMemberId = teamMemberId
                });
            // Broad invalidate matching React invalidateAppQueries(['teamMembers', 'projects']).
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

    void OnIgnoreSelectedWorkItems()
    {
        if (_selectedWorkItems.Count == 0) return;
        _importWorkItems = _importWorkItems
            .Where(item => item.Id is null || !_selectedWorkItems.Contains(item.Id.Value))
            .ToList();
        _selectedWorkItems = [];
    }

    public void Dispose() => Cache.Changed -= OnCacheChanged;
}
