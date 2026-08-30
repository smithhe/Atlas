using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberOverviewTab
{
    [Inject] private AppCacheService Cache { get; set; } = null!;

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

    private bool _profileOpen, _signalsOpen, _focusOpen;
    private string _profileName = "", _profileRole = "", _profileTz = "", _profileHours = "", _focusDraft = "";
    private LoadSignal _draftLoad;
    private DeliverySignal _draftDelivery;
    private SupportNeededSignal _draftSupport;

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
        _profileName = Member.Name ?? "";
        _profileRole = Member.Role ?? "";
        _profileTz = Member.Profile.TimeZone ?? "";
        _profileHours = Member.Profile.TypicalHours ?? "";
        _profileOpen = true;
    }

    private void CloseProfileModal()
    {
        _profileOpen = false;
        _profileName = _profileRole = _profileTz = _profileHours = "";
    }

    private async Task SaveProfile()
    {
        TeamMember next = CloneMember(Member);
        next.Name = string.IsNullOrWhiteSpace(_profileName) ? Member.Name : _profileName.Trim();
        next.Role = string.IsNullOrWhiteSpace(_profileRole) ? null : _profileRole.Trim();
        next.Profile = new TeamMemberProfile
        {
            TimeZone = string.IsNullOrWhiteSpace(_profileTz) ? null : _profileTz.Trim(),
            TypicalHours = string.IsNullOrWhiteSpace(_profileHours) ? null : _profileHours.Trim()
        };
        await OnUpdate.InvokeAsync(next);
        CloseProfileModal();
    }

    private void OpenSignalsModal()
    {
        _draftLoad = Member.Signals.Load;
        _draftDelivery = Member.Signals.Delivery;
        _draftSupport = Member.Signals.SupportNeeded;
        _signalsOpen = true;
    }

    private async Task SaveSignals()
    {
        TeamMember next = CloneMember(Member);
        next.Signals = new TeamMemberSignals
        {
            Load = _draftLoad,
            Delivery = _draftDelivery,
            SupportNeeded = _draftSupport
        };
        await OnUpdate.InvokeAsync(next);
        _signalsOpen = false;
    }

    private void OpenFocusModal()
    {
        _focusDraft = Member.CurrentFocus ?? "";
        _focusOpen = true;
    }

    private void CloseFocusModal()
    {
        _focusOpen = false;
        _focusDraft = "";
    }

    private async Task SaveFocus()
    {
        TeamMember next = CloneMember(Member);
        next.CurrentFocus = _focusDraft.Trim();
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
