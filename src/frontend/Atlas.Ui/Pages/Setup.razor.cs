using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Setup
    {
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;
        [Inject] private IAzureDevOpsService _azureDevOpsService { get; set; } = null!;

        private enum SetupStep { Project, Team, Members, Saving }

        private ElementReference HeaderRef { get; set; }
        private ElementReference ProjectsCardRef { get; set; }
        private ElementReference TeamsCardRef { get; set; }
        private ElementReference AreaPathCardRef { get; set; }
        private ElementReference MembersCardRef { get; set; }

        private int ProjectsScrollGen { get; set; }
        private int TeamsScrollGen { get; set; }
        private int MembersScrollGen { get; set; }
        private int ScrolledProjectsGen { get; set; } = -1;
        private int ScrolledTeamsGen { get; set; } = -1;
        private int ScrolledMembersGen { get; set; } = -1;

        private SetupInterop? SetupInteropRef { get; set; }

        private string Organization { get; set; } = "";
        private string AreaPath { get; set; } = "";
        private bool AreaPathLoading { get; set; }
        private string? AreaPathError { get; set; }

        private List<AzureProject> Projects { get; set; } = [];
        private string SelectedProjectId { get; set; } = "";
        private List<AzureTeam> Teams { get; set; } = [];
        private string SelectedTeamId { get; set; } = "";
        private List<AzureUser> Users { get; set; } = [];
        private HashSet<string> SelectedUsers { get; set; } = new(StringComparer.Ordinal);

        private string ProjectQuery { get; set; } = "";
        private string TeamQuery { get; set; } = "";
        private string MemberQuery { get; set; } = "";

        private SetupStep Step { get; set; } = SetupStep.Project;
        private bool Loading { get; set; }
        private string? Error { get; set; }

        private string StepLabel => this.Step switch
        {
            SetupStep.Saving => "Saving",
            SetupStep.Project => "Step 1/3 • Project",
            SetupStep.Team => "Step 2/3 • Team",
            _ => "Step 3/3 • Members"
        };

        private AzureProject? SelectedProject =>
            this.Projects.FirstOrDefault(p => p.Id == this.SelectedProjectId);

        private AzureTeam? SelectedTeam =>
            this.Teams.FirstOrDefault(t => t.Id == this.SelectedTeamId);

        private List<AzureProject> FilteredProjects
        {
            get
            {
                IEnumerable<AzureProject> sorted = this.Projects
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
                var q = this.ProjectQuery.Trim();
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
                IEnumerable<AzureTeam> sorted = this.Teams
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase);
                var q = this.TeamQuery.Trim();
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
                IEnumerable<AzureUser> sorted = this.Users
                    .OrderBy(u => (u.DisplayName ?? u.UniqueName ?? ""), StringComparer.OrdinalIgnoreCase);
                var q = this.MemberQuery.Trim();
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
                AzureConnection? conn = await this._azureDevOpsService.TryGetConnectionAsync();
                if (!string.IsNullOrWhiteSpace(conn?.Organization))
                {
                    this.Organization = conn.Organization;
                }
            }
            catch
            {
                // Setup handles missing connection.
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                this.SetupInteropRef = new SetupInterop(this._js);
            }

            if (this.SetupInteropRef is null)
            {
                return;
            }

            try
            {
                if (this.Projects.Count > 0 && this.ProjectsScrollGen != this.ScrolledProjectsGen)
                {
                    this.ScrolledProjectsGen = this.ProjectsScrollGen;
                    await this.SetupInteropRef.ScrollToCardAsync(this.HeaderRef, this.ProjectsCardRef);
                }

                if (this.Step == SetupStep.Team && this.Teams.Count > 0 && this.TeamsScrollGen != this.ScrolledTeamsGen)
                {
                    this.ScrolledTeamsGen = this.TeamsScrollGen;
                    await this.SetupInteropRef.ScrollToCardAsync(this.HeaderRef, this.TeamsCardRef);
                }

                if (this.Step == SetupStep.Members && this.MembersScrollGen != this.ScrolledMembersGen)
                {
                    this.ScrolledMembersGen = this.MembersScrollGen;
                    if (this.Users.Count > 0)
                    {
                        await this.SetupInteropRef.ScrollToCardAsync(this.HeaderRef, this.MembersCardRef);
                    }
                    else if (!string.IsNullOrEmpty(this.SelectedTeamId))
                    {
                        await this.SetupInteropRef.ScrollToCardAsync(this.HeaderRef, this.AreaPathCardRef);
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

        private void Skip() => this._nav.NavigateTo("/dashboard");

        private void OnOrganizationInput(ChangeEventArgs e) => this.Organization = e.Value?.ToString() ?? "";
        private void OnProjectQueryInput(ChangeEventArgs e) => this.ProjectQuery = e.Value?.ToString() ?? "";
        private void OnTeamQueryInput(ChangeEventArgs e) => this.TeamQuery = e.Value?.ToString() ?? "";
        private void OnMemberQueryInput(ChangeEventArgs e) => this.MemberQuery = e.Value?.ToString() ?? "";

        private async Task OnLoadProjects()
        {
            if (string.IsNullOrWhiteSpace(this.Organization) || this.Loading)
            {
                return;
            }

            this.Loading = true;
            this.Error = null;
            try
            {
                IReadOnlyList<AzureProject> list = await this._azureDevOpsService.ListProjectsAsync(this.Organization);
                this.Projects = list.ToList();
                this.SelectedProjectId = "";
                this.SelectedTeamId = "";
                this.Teams = [];
                this.Users = [];
                this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);
                this.ProjectQuery = "";
                this.TeamQuery = "";
                this.MemberQuery = "";
                this.AreaPath = "";
                this.AreaPathError = null;
                this.AreaPathLoading = false;
                this.Step = SetupStep.Project;
                this.ProjectsScrollGen++;
            }
            catch (Exception ex)
            {
                this.Error = string.IsNullOrWhiteSpace(ex.Message) ? "Failed to load projects" : ex.Message;
            }
            finally
            {
                this.Loading = false;
            }
        }

        private async Task OnSelectProject(string projectId)
        {
            if (this.Loading)
            {
                return;
            }

            this.SelectedProjectId = projectId;
            this.SelectedTeamId = "";
            this.Teams = [];
            this.Users = [];
            this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);
            this.TeamQuery = "";
            this.MemberQuery = "";
            this.AreaPath = "";
            this.AreaPathError = null;
            this.AreaPathLoading = false;
            this.Error = null;
            this.Loading = true;
            try
            {
                IReadOnlyList<AzureTeam> list = await this._azureDevOpsService.ListTeamsAsync(this.Organization, projectId);
                this.Teams = list.ToList();
                this.Step = SetupStep.Team;
                this.TeamsScrollGen++;
            }
            catch (Exception ex)
            {
                this.Error = string.IsNullOrWhiteSpace(ex.Message) ? "Failed to load teams" : ex.Message;
            }
            finally
            {
                this.Loading = false;
            }
        }

        private async Task OnSelectTeam(string teamId, string teamName)
        {
            if (this.Loading)
            {
                return;
            }

            this.SelectedTeamId = teamId;
            this.Users = [];
            this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);
            this.MemberQuery = "";
            this.Error = null;
            this.Loading = true;
            this.AreaPathLoading = true;
            this.AreaPathError = null;
            try
            {
                Task<IReadOnlyList<AzureUser>> usersTask =
                    this._azureDevOpsService.ListUsersAsync(this.Organization, this.SelectedProjectId, teamId);
                Task<AzureTeamAreaPaths> areasTask =
                    this._azureDevOpsService.ListTeamAreaPathsAsync(this.Organization, this.SelectedProjectId, teamName);

                IReadOnlyList<AzureUser> users;
                try
                {
                    users = await usersTask;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message, ex);
                }

                this.Users = users.ToList();

                try
                {
                    AzureTeamAreaPaths areas = await areasTask;
                    var defaultValue = (areas.DefaultValue ?? "").Trim();
                    if (defaultValue.Length > 0)
                    {
                        this.AreaPath = defaultValue;
                    }
                    else if (areas.Values.Count > 0)
                    {
                        this.AreaPath = areas.Values.First().Value ?? "";
                    }
                    else
                    {
                        this.AreaPath = "";
                    }
                }
                catch (Exception ex)
                {
                    this.AreaPathError = ex.Message;
                }

                this.Step = SetupStep.Members;
                this.MembersScrollGen++;
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
            finally
            {
                this.Loading = false;
                this.AreaPathLoading = false;
            }
        }

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

        private void SelectAllShown()
        {
            this.SelectedUsers = FilteredUsers
                .Select(u => u.UniqueName)
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToHashSet(StringComparer.Ordinal);
        }

        private void ClearSelectedUsers() => this.SelectedUsers = new HashSet<string>(StringComparer.Ordinal);

        private async Task OnSave()
        {
            AzureProject? project = SelectedProject;
            AzureTeam? team = SelectedTeam;
            if (project is null || team is null || this.Loading)
            {
                return;
            }

            this.Loading = true;
            this.Error = null;
            this.Step = SetupStep.Saving;
            try
            {
                await this._azureDevOpsService.UpdateConnectionAsync(new AzureUpdateConnection
                {
                    Organization = this.Organization,
                    Project = project.Name,
                    AreaPath = this.AreaPath,
                    TeamName = team.Name,
                    IsEnabled = true,
                    ProjectId = project.Id,
                    TeamId = team.Id
                });

                var selected = this.Users
                    .Where(u => u.UniqueName is not null && this.SelectedUsers.Contains(u.UniqueName))
                    .ToList();

                if (selected.Count > 0)
                {
                    await this._azureDevOpsService.ImportTeamAsync(selected);
                    await this._cache.RefetchTeamAsync();
                }

                this._nav.NavigateTo("/dashboard");
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
                this.Step = SetupStep.Members;
            }
            finally
            {
                this.Loading = false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.SetupInteropRef is not null)
            {
                await this.SetupInteropRef.DisposeAsync();
                this.SetupInteropRef = null;
            }
        }
    }
}
