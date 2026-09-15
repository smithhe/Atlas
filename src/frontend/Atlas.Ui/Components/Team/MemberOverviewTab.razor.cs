using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Team
{
    public partial class MemberOverviewTab
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;

        [Parameter, EditorRequired] public TeamMember Member { get; set; } = null!;
        [Parameter] public EventCallback<TeamMember> OnUpdate { get; set; }
        [Parameter] public EventCallback OnGoToNotes { get; set; }
        [Parameter] public EventCallback<string> OnGoToWorkItem { get; set; }

        private static readonly (string Value, string Label)[] TimeZones =
        [
            ("PT", "Pacific (PT)"),
            ("MT", "Mountain (MT)"),
            ("CT", "Central (CT)"),
            ("ET", "Eastern (ET)"),
            ("AK", "Alaska (AK)"),
            ("HI", "Hawaii (HI)"),
            ("AZ", "Arizona (MT-noDST)")
        ];

        private static readonly LoadSignal[] LoadOptions = [LoadSignal.Light, LoadSignal.Normal, LoadSignal.Heavy];
        private static readonly DeliverySignal[] DeliveryOptions = [DeliverySignal.AtRisk, DeliverySignal.OnTrack, DeliverySignal.Blocked];
        private static readonly SupportNeededSignal[] SupportOptions = [SupportNeededSignal.Low, SupportNeededSignal.Medium, SupportNeededSignal.High];

        private bool ProfileOpen { get; set; }
        private bool SignalsOpen { get; set; }
        private bool FocusOpen { get; set; }
            private string ProfileName { get; set; } = "";
        private string ProfileRole { get; set; } = "";
        private string ProfileTz { get; set; } = "";
        private string ProfileHours { get; set; } = "";
        private string FocusDraft { get; set; } = "";
        private LoadSignal DraftLoad { get; set; }
        private DeliverySignal DraftDelivery { get; set; }
        private SupportNeededSignal DraftSupport { get; set; }

        private List<AzureItem> CurrentTickets =>
            Member.AzureItems.Where(a => TeamLogic.IsCurrentTicketStatus(a.Status)).ToList();

        private List<AzureItem> TopCurrentTickets => CurrentTickets.Take(3).ToList();

        private List<TeamNote> PinnedNotes
        {
            get
            {
                var byId = Member.Notes.ToDictionary(n => n.Id);
                return Member.PinnedNoteIds
                    .Select(id => byId.TryGetValue(id, out TeamNote? n) ? n : null)
                    .Where(n => n is not null)
                    .Cast<TeamNote>()
                    .Take(3)
                    .ToList();
            }
        }

        private static string GetPreview(TeamNote note)
        {
            var text = System.Text.RegularExpressions.Regex.Replace(note.Text, @"\s+", " ").Trim();
            return text.Length <= 120 ? text : text[..120].Trim() + "…";
        }

        private void OpenProfileModal()
        {
            this.ProfileName = Member.Name ?? "";
            this.ProfileRole = Member.Role ?? "";
            this.ProfileTz = Member.Profile.TimeZone ?? "";
            this.ProfileHours = Member.Profile.TypicalHours ?? "";
            this.ProfileOpen = true;
        }

        private void CloseProfileModal()
        {
            this.ProfileOpen = false;
            this.ProfileName = this.ProfileRole = this.ProfileTz = this.ProfileHours = "";
        }

        private async Task SaveProfile()
        {
            TeamMember next = CloneMember(Member);
            next.Name = string.IsNullOrWhiteSpace(this.ProfileName) ? Member.Name : this.ProfileName.Trim();
            next.Role = string.IsNullOrWhiteSpace(this.ProfileRole) ? null : this.ProfileRole.Trim();
            next.Profile = new TeamMemberProfile
            {
                TimeZone = string.IsNullOrWhiteSpace(this.ProfileTz) ? null : this.ProfileTz.Trim(),
                TypicalHours = string.IsNullOrWhiteSpace(this.ProfileHours) ? null : this.ProfileHours.Trim()
            };
            await OnUpdate.InvokeAsync(next);
            CloseProfileModal();
        }

        private void OpenSignalsModal()
        {
            this.DraftLoad = Member.Signals.Load;
            this.DraftDelivery = Member.Signals.Delivery;
            this.DraftSupport = Member.Signals.SupportNeeded;
            this.SignalsOpen = true;
        }

        private async Task SaveSignals()
        {
            TeamMember next = CloneMember(Member);
            next.Signals = new TeamMemberSignals
            {
                Load = this.DraftLoad,
                Delivery = this.DraftDelivery,
                SupportNeeded = this.DraftSupport
            };
            await OnUpdate.InvokeAsync(next);
            this.SignalsOpen = false;
        }

        private void OpenFocusModal()
        {
            this.FocusDraft = Member.CurrentFocus ?? "";
            this.FocusOpen = true;
        }

        private void CloseFocusModal()
        {
            this.FocusOpen = false;
            this.FocusDraft = "";
        }

        private async Task SaveFocus()
        {
            TeamMember next = CloneMember(Member);
            next.CurrentFocus = this.FocusDraft.Trim();
            await OnUpdate.InvokeAsync(next);
            CloseFocusModal();
        }

        private async Task CycleLoad()
        {
            TeamMember next = CloneMember(Member);
            next.Signals = new TeamMemberSignals
            {
                Load = Cycle(Member.Signals.Load, LoadOptions),
                Delivery = Member.Signals.Delivery,
                SupportNeeded = Member.Signals.SupportNeeded
            };
            await OnUpdate.InvokeAsync(next);
        }

        private async Task CycleDelivery()
        {
            TeamMember next = CloneMember(Member);
            next.Signals = new TeamMemberSignals
            {
                Load = Member.Signals.Load,
                Delivery = Cycle(Member.Signals.Delivery, DeliveryOptions),
                SupportNeeded = Member.Signals.SupportNeeded
            };
            await OnUpdate.InvokeAsync(next);
        }

        private async Task CycleSupport()
        {
            TeamMember next = CloneMember(Member);
            next.Signals = new TeamMemberSignals
            {
                Load = Member.Signals.Load,
                Delivery = Member.Signals.Delivery,
                SupportNeeded = Cycle(Member.Signals.SupportNeeded, SupportOptions)
            };
            await OnUpdate.InvokeAsync(next);
        }

        private static T Cycle<T>(T value, T[] options) where T : struct
        {
            var idx = Array.IndexOf(options, value);
            if (idx < 0)
            {
                return options[0];
            }

            return options[(idx + 1) % options.Length];
        }

        private static TeamMember CloneMember(TeamMember m) => EntityClone.TeamMember(m);
    }
}
