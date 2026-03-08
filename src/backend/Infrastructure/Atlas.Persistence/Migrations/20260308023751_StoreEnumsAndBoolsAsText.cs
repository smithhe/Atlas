using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreEnumsAndBoolsAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AzureConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Organization = table.Column<string>(type: "text", nullable: false),
                    Project = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    AreaPath = table.Column<string>(type: "text", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: true),
                    TeamId = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<string>(type: "character varying(5)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AzureUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    UniqueName = table.Column<string>(type: "text", nullable: false),
                    Descriptor = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<string>(type: "character varying(5)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrowthPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusAreasMarkdown = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductOwners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOwners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaleDays = table.Column<int>(type: "integer", nullable: false),
                    DefaultAiManualOnly = table.Column<string>(type: "character varying(5)", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    AzureDevOpsBaseUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    StatusDot = table.Column<string>(type: "text", nullable: false),
                    CurrentFocus = table.Column<string>(type: "text", nullable: false),
                    Profile_TimeZone = table.Column<string>(type: "text", nullable: true),
                    Profile_TypicalHours = table.Column<string>(type: "text", nullable: true),
                    Signals_Load = table.Column<string>(type: "text", nullable: false),
                    Signals_Delivery = table.Column<string>(type: "text", nullable: false),
                    Signals_SupportNeeded = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AzureSyncStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzureConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSuccessfulChangedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSuccessfulWorkItemId = table.Column<int>(type: "integer", nullable: true),
                    LastAttemptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastRunStatus = table.Column<string>(type: "text", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureSyncStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzureSyncStates_AzureConnections_AzureConnectionId",
                        column: x => x.AzureConnectionId,
                        principalTable: "AzureConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzureWorkItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzureConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkItemId = table.Column<int>(type: "integer", nullable: false),
                    Rev = table.Column<int>(type: "integer", nullable: false),
                    ChangedDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    WorkItemType = table.Column<string>(type: "text", nullable: false),
                    AreaPath = table.Column<string>(type: "text", nullable: false),
                    IterationPath = table.Column<string>(type: "text", nullable: false),
                    AssignedToUniqueName = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureWorkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzureWorkItems_AzureConnections_AzureConnectionId",
                        column: x => x.AzureConnectionId,
                        principalTable: "AzureConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrowthFeedbackThemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrowthId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ObservedSinceLabel = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthFeedbackThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrowthFeedbackThemes_GrowthPlans_GrowthId",
                        column: x => x.GrowthId,
                        principalTable: "GrowthPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrowthGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrowthId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    SuccessCriteria = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrowthGoals_GrowthPlans_GrowthId",
                        column: x => x.GrowthId,
                        principalTable: "GrowthPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrowthSkillsInProgress",
                columns: table => new
                {
                    GrowthId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthSkillsInProgress", x => new { x.GrowthId, x.Value });
                    table.ForeignKey(
                        name: "FK_GrowthSkillsInProgress_GrowthPlans_GrowthId",
                        column: x => x.GrowthId,
                        principalTable: "GrowthPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Health = table.Column<string>(type: "text", nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    ProductOwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LatestCheckIn_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    LatestCheckIn_Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_ProductOwners_ProductOwnerId",
                        column: x => x.ProductOwnerId,
                        principalTable: "ProductOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AzureUserMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzureUniqueName = table.Column<string>(type: "text", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureUserMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzureUserMappings_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false),
                    PinnedOrder = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamNotes_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrowthGoalActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrowthGoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthGoalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrowthGoalActions_GrowthGoals_GrowthGoalId",
                        column: x => x.GrowthGoalId,
                        principalTable: "GrowthGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrowthGoalCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrowthGoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Signal = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthGoalCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrowthGoalCheckIns_GrowthGoals_GrowthGoalId",
                        column: x => x.GrowthGoalId,
                        principalTable: "GrowthGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzureWorkItemLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzureWorkItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureWorkItemLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzureWorkItemLinks_AzureWorkItems_AzureWorkItemId",
                        column: x => x.AzureWorkItemId,
                        principalTable: "AzureWorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AzureWorkItemLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AzureWorkItemLinks_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectLinks",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLinks", x => new { x.ProjectId, x.Label, x.Url });
                    table.ForeignKey(
                        name: "FK_ProjectLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTags",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTags", x => new { x.ProjectId, x.Value });
                    table.ForeignKey(
                        name: "FK_ProjectTags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeamMembers",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeamMembers", x => new { x.ProjectId, x.TeamMemberId });
                    table.ForeignKey(
                        name: "FK_ProjectTeamMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeamMembers_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Risks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RiskHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskHistoryEntries_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskTeamMembers",
                columns: table => new
                {
                    RiskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskTeamMembers", x => new { x.RiskId, x.TeamMemberId });
                    table.ForeignKey(
                        name: "FK_RiskTeamMembers_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiskTeamMembers_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EstimatedDurationText = table.Column<string>(type: "text", nullable: false),
                    EstimateConfidence = table.Column<string>(type: "text", nullable: false),
                    ActualDurationText = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    LastTouchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tasks_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamMemberRisks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    RiskType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Trend = table.Column<string>(type: "text", nullable: false),
                    FirstNoticedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImpactArea = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CurrentAction = table.Column<string>(type: "text", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LinkedGlobalRiskId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMemberRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMemberRisks_Risks_LinkedGlobalRiskId",
                        column: x => x.LinkedGlobalRiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamMemberRisks_TeamMembers_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DependentTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerTaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Tasks_BlockerTaskId",
                        column: x => x.BlockerTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Tasks_DependentTaskId",
                        column: x => x.DependentTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AzureSyncStates_AzureConnectionId",
                table: "AzureSyncStates",
                column: "AzureConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzureUserMappings_AzureUniqueName",
                table: "AzureUserMappings",
                column: "AzureUniqueName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzureUserMappings_TeamMemberId",
                table: "AzureUserMappings",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AzureUsers_UniqueName",
                table: "AzureUsers",
                column: "UniqueName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzureWorkItemLinks_AzureWorkItemId",
                table: "AzureWorkItemLinks",
                column: "AzureWorkItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzureWorkItemLinks_ProjectId",
                table: "AzureWorkItemLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AzureWorkItemLinks_TeamMemberId",
                table: "AzureWorkItemLinks",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AzureWorkItems_AzureConnectionId_WorkItemId",
                table: "AzureWorkItems",
                columns: new[] { "AzureConnectionId", "WorkItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrowthFeedbackThemes_GrowthId",
                table: "GrowthFeedbackThemes",
                column: "GrowthId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthGoalActions_GrowthGoalId",
                table: "GrowthGoalActions",
                column: "GrowthGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthGoalCheckIns_GrowthGoalId_Date",
                table: "GrowthGoalCheckIns",
                columns: new[] { "GrowthGoalId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_GrowthGoals_GrowthId",
                table: "GrowthGoals",
                column: "GrowthId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthPlans_TeamMemberId",
                table: "GrowthPlans",
                column: "TeamMemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrowthSkillsInProgress_GrowthId_SortOrder",
                table: "GrowthSkillsInProgress",
                columns: new[] { "GrowthId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProductOwnerId",
                table: "Projects",
                column: "ProductOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeamMembers_TeamMemberId",
                table: "ProjectTeamMembers",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskHistoryEntries_RiskId",
                table: "RiskHistoryEntries",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ProjectId",
                table: "Risks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTeamMembers_TeamMemberId",
                table: "RiskTeamMembers",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_BlockerTaskId",
                table: "TaskDependencies",
                column: "BlockerTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_DependentTaskId_BlockerTaskId",
                table: "TaskDependencies",
                columns: new[] { "DependentTaskId", "BlockerTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RiskId",
                table: "Tasks",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberRisks_LinkedGlobalRiskId",
                table: "TeamMemberRisks",
                column: "LinkedGlobalRiskId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberRisks_TeamMemberId",
                table: "TeamMemberRisks",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamNotes_TeamMemberId_PinnedOrder",
                table: "TeamNotes",
                columns: new[] { "TeamMemberId", "PinnedOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AzureSyncStates");

            migrationBuilder.DropTable(
                name: "AzureUserMappings");

            migrationBuilder.DropTable(
                name: "AzureUsers");

            migrationBuilder.DropTable(
                name: "AzureWorkItemLinks");

            migrationBuilder.DropTable(
                name: "GrowthFeedbackThemes");

            migrationBuilder.DropTable(
                name: "GrowthGoalActions");

            migrationBuilder.DropTable(
                name: "GrowthGoalCheckIns");

            migrationBuilder.DropTable(
                name: "GrowthSkillsInProgress");

            migrationBuilder.DropTable(
                name: "ProjectLinks");

            migrationBuilder.DropTable(
                name: "ProjectTags");

            migrationBuilder.DropTable(
                name: "ProjectTeamMembers");

            migrationBuilder.DropTable(
                name: "RiskHistoryEntries");

            migrationBuilder.DropTable(
                name: "RiskTeamMembers");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "TaskDependencies");

            migrationBuilder.DropTable(
                name: "TeamMemberRisks");

            migrationBuilder.DropTable(
                name: "TeamNotes");

            migrationBuilder.DropTable(
                name: "AzureWorkItems");

            migrationBuilder.DropTable(
                name: "GrowthGoals");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "AzureConnections");

            migrationBuilder.DropTable(
                name: "GrowthPlans");

            migrationBuilder.DropTable(
                name: "Risks");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "ProductOwners");
        }
    }
}
