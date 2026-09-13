using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Setup
{
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private AzureDevOpsService AzureDevOpsService { get; set; } = null!;

    private enum SetupStep { Project, Team, Members, Saving }

    private ElementReference _headerRef;
    private ElementReference _projectsCardRef;
    private ElementReference _teamsCardRef;
    private ElementReference _areaPathCardRef;
    private ElementReference _membersCardRef;

    private int _projectsScrollGen;
    private int _teamsScrollGen;
    private int _membersScrollGen;
    private int _scrolledProjectsGen = -1;
    private int _scrolledTeamsGen = -1;
    private int _scrolledMembersGen = -1;

    private string _organization = "";
    private string _areaPath = "";
    private bool _areaPathLoading;
    private string? _areaPathError;

    private List<AzureProject> _projects = [];
    private string _selectedProjectId = "";
    private List<AzureTeam> _teams = [];
    private string _selectedTeamId = "";
    private List<AzureUser> _users = [];
    private HashSet<string> _selectedUsers = new(StringComparer.Ordinal);

    private string _projectQuery = "";
    private string _teamQuery = "";
    private string _memberQuery = "";

    private SetupStep _step = SetupStep.Project;
    private bool _loading;
    private string? _error;

    private string StepLabel => _step switch
    {
        SetupStep.Saving => "Saving",
        SetupStep.Project => "Step 1/3 • Project",
        SetupStep.Team => "Step 2/3 • Team",
        _ => "Step 3/3 • Members"
    };

    private AzureProject? SelectedProject =>
        _projects.FirstOrDefault(p => p.Id == _selectedProjectId);

    private AzureTeam? SelectedTeam =>
        _teams.FirstOrDefault(t => t.Id == _selectedTeamId);

    private List<AzureProject> FilteredProjects
    {
        get
        {
            IEnumerable<AzureProject> sorted = _projects
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
            var q = _projectQuery.Trim();
            if (q.Length == 0)
            {
                return sorted.ToList();
            }

            return sorted
                .Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private List<AzureTeam> FilteredTeams
    {
        get
        {
            IEnumerable<AzureTeam> sorted = _teams
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);
            var q = _teamQuery.Trim();
            if (q.Length == 0)
            {
                return sorted.ToList();
            }

            return sorted
                .Where(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private List<AzureUser> FilteredUsers
    {
        get
        {
            IEnumerable<AzureUser> sorted = _users
                .OrderBy(u => (u.DisplayName ?? u.UniqueName ?? ""), StringComparer.OrdinalIgnoreCase);
            var q = _memberQuery.Trim();
            if (q.Length == 0)
            {
                return sorted.ToList();
            }

            return sorted
                .Where(u =>
                {
                    var hay = $"{u.DisplayName ?? ""} {u.UniqueName ?? ""}";
                    return hay.Contains(q, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            AzureConnection? conn = await AzureDevOpsService.TryGetConnectionAsync();
            if (!string.IsNullOrWhiteSpace(conn?.Organization))
            {
                _organization = conn.Organization;
            }
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
                {
                    await Js.InvokeVoidAsync("atlasUi.scrollToCard", _headerRef, _membersCardRef);
                }
                else if (!string.IsNullOrEmpty(_selectedTeamId))
                {
                    await Js.InvokeVoidAsync("atlasUi.scrollToCard", _headerRef, _areaPathCardRef);
                }
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

    private void Skip() => Nav.NavigateTo("/dashboard");

    private void OnOrganizationInput(ChangeEventArgs e) => _organization = e.Value?.ToString() ?? "";
    private void OnProjectQueryInput(ChangeEventArgs e) => _projectQuery = e.Value?.ToString() ?? "";
    private void OnTeamQueryInput(ChangeEventArgs e) => _teamQuery = e.Value?.ToString() ?? "";
    private void OnMemberQueryInput(ChangeEventArgs e) => _memberQuery = e.Value?.ToString() ?? "";

    private async Task OnLoadProjects()
    {
        if (string.IsNullOrWhiteSpace(_organization) || _loading)
        {
            return;
        }

        _loading = true;
        _error = null;
        try
        {
            IReadOnlyList<AzureProject> list = await AzureDevOpsService.ListProjectsAsync(_organization);
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

    private async Task OnSelectProject(string projectId)
    {
        if (_loading)
        {
            return;
        }

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
            IReadOnlyList<AzureTeam> list = await AzureDevOpsService.ListTeamsAsync(_organization, projectId);
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

    private async Task OnSelectTeam(string teamId, string teamName)
    {
        if (_loading)
        {
            return;
        }

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
            Task<IReadOnlyList<AzureUser>> usersTask =
                AzureDevOpsService.ListUsersAsync(_organization, _selectedProjectId, teamId);
            Task<AzureTeamAreaPaths> areasTask =
                AzureDevOpsService.ListTeamAreaPathsAsync(_organization, _selectedProjectId, teamName);

            IReadOnlyList<AzureUser> users;
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
                AzureTeamAreaPaths areas = await areasTask;
                var defaultValue = (areas.DefaultValue ?? "").Trim();
                if (defaultValue.Length > 0)
                {
                    _areaPath = defaultValue;
                }
                else if (areas.Values.Count > 0)
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

    private void SelectAllShown()
    {
        _selectedUsers = FilteredUsers
            .Select(u => u.UniqueName)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private void ClearSelectedUsers() => _selectedUsers = new HashSet<string>(StringComparer.Ordinal);

    private async Task OnSave()
    {
        AzureProject? project = SelectedProject;
        AzureTeam? team = SelectedTeam;
        if (project is null || team is null || _loading)
        {
            return;
        }

        _loading = true;
        _error = null;
        _step = SetupStep.Saving;
        try
        {
            await AzureDevOpsService.UpdateConnectionAsync(new AzureUpdateConnection
            {
                Organization = _organization,
                Project = project.Name,
                AreaPath = _areaPath,
                TeamName = team.Name,
                IsEnabled = true,
                ProjectId = project.Id,
                TeamId = team.Id
            });

            var selected = _users
                .Where(u => u.UniqueName is not null && _selectedUsers.Contains(u.UniqueName))
                .ToList();

            if (selected.Count > 0)
            {
                await AzureDevOpsService.ImportTeamAsync(selected);
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
