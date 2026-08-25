using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Setup
{
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] IJSRuntime Js { get; set; } = default!;
    [Inject] AzureDevOpsService AzureDevOpsService { get; set; } = default!;

    enum SetupStep { Project, Team, Members, Saving }

    ElementReference _headerRef;
    ElementReference _projectsCardRef;
    ElementReference _teamsCardRef;
    ElementReference _areaPathCardRef;
    ElementReference _membersCardRef;

    int _projectsScrollGen;
    int _teamsScrollGen;
    int _membersScrollGen;
    int _scrolledProjectsGen = -1;
    int _scrolledTeamsGen = -1;
    int _scrolledMembersGen = -1;

    string _organization = "";
    string _areaPath = "";
    bool _areaPathLoading;
    string? _areaPathError;

    List<AtlasApiDTOsAzureDevOpsAzureProjectDto> _projects = [];
    string _selectedProjectId = "";
    List<AtlasApiDTOsAzureDevOpsAzureTeamDto> _teams = [];
    string _selectedTeamId = "";
    List<AtlasApiDTOsAzureDevOpsAzureUserDto> _users = [];
    HashSet<string> _selectedUsers = new(StringComparer.Ordinal);

    string _projectQuery = "";
    string _teamQuery = "";
    string _memberQuery = "";

    SetupStep _step = SetupStep.Project;
    bool _loading;
    string? _error;

    string StepLabel => _step switch
    {
        SetupStep.Saving => "Saving",
        SetupStep.Project => "Step 1/3 • Project",
        SetupStep.Team => "Step 2/3 • Team",
        _ => "Step 3/3 • Members"
    };

    AtlasApiDTOsAzureDevOpsAzureProjectDto? SelectedProject =>
        _projects.FirstOrDefault(p => p.Id == _selectedProjectId);

    AtlasApiDTOsAzureDevOpsAzureTeamDto? SelectedTeam =>
        _teams.FirstOrDefault(t => t.Id == _selectedTeamId);

    List<AtlasApiDTOsAzureDevOpsAzureProjectDto> FilteredProjects
    {
        get
        {
            IEnumerable<AtlasApiDTOsAzureDevOpsAzureProjectDto> sorted = _projects
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
            string q = _projectQuery.Trim();
            if (q.Length == 0) return sorted.ToList();
            return sorted
                .Where(p => (p.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    List<AtlasApiDTOsAzureDevOpsAzureTeamDto> FilteredTeams
    {
        get
        {
            IEnumerable<AtlasApiDTOsAzureDevOpsAzureTeamDto> sorted = _teams
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);
            string q = _teamQuery.Trim();
            if (q.Length == 0) return sorted.ToList();
            return sorted
                .Where(t => (t.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    List<AtlasApiDTOsAzureDevOpsAzureUserDto> FilteredUsers
    {
        get
        {
            IEnumerable<AtlasApiDTOsAzureDevOpsAzureUserDto> sorted = _users
                .OrderBy(u => (u.DisplayName ?? u.UniqueName ?? ""), StringComparer.OrdinalIgnoreCase);
            string q = _memberQuery.Trim();
            if (q.Length == 0) return sorted.ToList();
            return sorted
                .Where(u =>
                {
                    string hay = $"{u.DisplayName ?? ""} {u.UniqueName ?? ""}";
                    return hay.Contains(q, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            AtlasApiDTOsAzureDevOpsAzureConnectionDto conn =
                await AzureDevOpsService.GetConnectionAsync();
            if (!string.IsNullOrWhiteSpace(conn.Organization))
                _organization = conn.Organization;
        }
        catch
        {
            // Setup handles missing connection.
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (_projects.Count > 0 && _projectsScrollGen != _scrolledProjectsGen)
            {
                _scrolledProjectsGen = _projectsScrollGen;
                await Js.InvokeVoidAsync("atlasUi.scrollToCard", _headerRef, _projectsCardRef);
            }

            if (_step == SetupStep.Team && _teams.Count > 0 && _teamsScrollGen != _scrolledTeamsGen)
            {
                _scrolledTeamsGen = _teamsScrollGen;
                await Js.InvokeVoidAsync("atlasUi.scrollToCard", _headerRef, _teamsCardRef);
            }

            if (_step == SetupStep.Members && _membersScrollGen != _scrolledMembersGen)
            {
                _scrolledMembersGen = _membersScrollGen;
                if (_users.Count > 0)
                    await Js.InvokeVoidAsync("atlasUi.scrollToCard", _headerRef, _membersCardRef);
                else if (!string.IsNullOrEmpty(_selectedTeamId))
                    await Js.InvokeVoidAsync("atlasUi.scrollToCard", _headerRef, _areaPathCardRef);
            }
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    void Skip() => Nav.NavigateTo("/dashboard");

    void OnOrganizationInput(ChangeEventArgs e) => _organization = e.Value?.ToString() ?? "";
    void OnProjectQueryInput(ChangeEventArgs e) => _projectQuery = e.Value?.ToString() ?? "";
    void OnTeamQueryInput(ChangeEventArgs e) => _teamQuery = e.Value?.ToString() ?? "";
    void OnMemberQueryInput(ChangeEventArgs e) => _memberQuery = e.Value?.ToString() ?? "";

    async Task OnLoadProjects()
    {
        if (string.IsNullOrWhiteSpace(_organization) || _loading) return;
        _loading = true;
        _error = null;
        try
        {
            ICollection<AtlasApiDTOsAzureDevOpsAzureProjectDto> list =
                await AzureDevOpsService.ListProjectsAsync(_organization);
            _projects = list.ToList();
            _selectedProjectId = "";
            _selectedTeamId = "";
            _teams = [];
            _users = [];
            _selectedUsers = new HashSet<string>(StringComparer.Ordinal);
            _projectQuery = "";
            _teamQuery = "";
            _memberQuery = "";
            _areaPath = "";
            _areaPathError = null;
            _areaPathLoading = false;
            _step = SetupStep.Project;
            _projectsScrollGen++;
        }
        catch (Exception ex)
        {
            _error = string.IsNullOrWhiteSpace(ex.Message) ? "Failed to load projects" : ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    async Task OnSelectProject(string projectId)
    {
        if (_loading) return;
        _selectedProjectId = projectId;
        _selectedTeamId = "";
        _teams = [];
        _users = [];
        _selectedUsers = new HashSet<string>(StringComparer.Ordinal);
        _teamQuery = "";
        _memberQuery = "";
        _areaPath = "";
        _areaPathError = null;
        _areaPathLoading = false;
        _error = null;
        _loading = true;
        try
        {
            ICollection<AtlasApiDTOsAzureDevOpsAzureTeamDto> list =
                await AzureDevOpsService.ListTeamsAsync(_organization, projectId);
            _teams = list.ToList();
            _step = SetupStep.Team;
            _teamsScrollGen++;
        }
        catch (Exception ex)
        {
            _error = string.IsNullOrWhiteSpace(ex.Message) ? "Failed to load teams" : ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    async Task OnSelectTeam(string teamId, string teamName)
    {
        if (_loading) return;
        _selectedTeamId = teamId;
        _users = [];
        _selectedUsers = new HashSet<string>(StringComparer.Ordinal);
        _memberQuery = "";
        _error = null;
        _loading = true;
        _areaPathLoading = true;
        _areaPathError = null;
        try
        {
            Task<ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto>> usersTask =
                AzureDevOpsService.ListUsersAsync(
                _organization, _selectedProjectId, teamId);
            Task<AtlasApiDTOsAzureDevOpsAzureTeamAreaPathsDto> areasTask =
                AzureDevOpsService.ListTeamAreaPathsAsync(
                _organization, _selectedProjectId, teamName);

            ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto> users;
            try
            {
                users = await usersTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }

            _users = users.ToList();

            try
            {
                AtlasApiDTOsAzureDevOpsAzureTeamAreaPathsDto areas = await areasTask;
                string defaultValue = (areas.DefaultValue ?? "").Trim();
                if (defaultValue.Length > 0)
                {
                    _areaPath = defaultValue;
                }
                else if (areas.Values?.Count > 0)
                {
                    _areaPath = areas.Values.First().Value ?? "";
                }
                else
                {
                    _areaPath = "";
                }
            }
            catch (Exception ex)
            {
                _areaPathError = ex.Message;
            }

            _step = SetupStep.Members;
            _membersScrollGen++;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
            _areaPathLoading = false;
        }
    }

    void ToggleUser(string uniqueName, ChangeEventArgs e)
    {
        HashSet<string> next = new(_selectedUsers, StringComparer.Ordinal);
        if (e.Value is bool b ? b : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            next.Add(uniqueName);
        else
            next.Remove(uniqueName);
        _selectedUsers = next;
    }

    void SelectAllShown()
    {
        _selectedUsers = FilteredUsers
            .Select(u => u.UniqueName)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToHashSet(StringComparer.Ordinal);
    }

    void ClearSelectedUsers() => _selectedUsers = new HashSet<string>(StringComparer.Ordinal);

    async Task OnSave()
    {
        AtlasApiDTOsAzureDevOpsAzureProjectDto? project = SelectedProject;
        AtlasApiDTOsAzureDevOpsAzureTeamDto? team = SelectedTeam;
        if (project is null || team is null || _loading) return;

        _loading = true;
        _error = null;
        _step = SetupStep.Saving;
        try
        {
            await AzureDevOpsService.UpdateConnectionAsync(
                new AtlasApiDTOsAzureDevOpsUpdateAzureConnectionRequest
                {
                    Organization = _organization,
                    Project = project.Name,
                    AreaPath = _areaPath,
                    TeamName = team.Name,
                    IsEnabled = true,
                    ProjectId = project.Id,
                    TeamId = team.Id
                });

            List<AtlasApiDTOsAzureDevOpsAzureUserSelectionDto> selected = _users
                .Where(u => u.UniqueName is not null && _selectedUsers.Contains(u.UniqueName))
                .Select(u => new AtlasApiDTOsAzureDevOpsAzureUserSelectionDto
                {
                    DisplayName = u.DisplayName,
                    UniqueName = u.UniqueName,
                    Descriptor = u.Descriptor
                })
                .ToList();

            if (selected.Count > 0)
            {
                await AzureDevOpsService.ImportTeamAsync(
                    new AtlasApiDTOsAzureDevOpsImportAzureTeamRequest { Users = selected });
                await Cache.RefetchTeamAsync();
            }

            Nav.NavigateTo("/dashboard");
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _step = SetupStep.Members;
        }
        finally
        {
            _loading = false;
        }
    }
}
