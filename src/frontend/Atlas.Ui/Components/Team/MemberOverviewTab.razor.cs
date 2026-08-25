using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Team;

public partial class MemberOverviewTab
{
    [Inject] AppCacheService Cache { get; set; } = default!;

    [Parameter, EditorRequired] public TeamMember Member { get; set; } = default!;
    [Parameter] public EventCallback<TeamMember> OnUpdate { get; set; }
    [Parameter] public EventCallback OnGoToNotes { get; set; }
    [Parameter] public EventCallback<string> OnGoToWorkItem { get; set; }

    static readonly (string Value, string Label)[] TimeZones =
    [
        ("PT", "Pacific (PT)"),
        ("MT", "Mountain (MT)"),
        ("CT", "Central (CT)"),
        ("ET", "Eastern (ET)"),
        ("AK", "Alaska (AK)"),
        ("HI", "Hawaii (HI)"),
        ("AZ", "Arizona (MT-noDST)")
    ];

    static readonly LoadSignal[] LoadOptions = [LoadSignal.Light, LoadSignal.Normal, LoadSignal.Heavy];
    static readonly DeliverySignal[] DeliveryOptions = [DeliverySignal.AtRisk, DeliverySignal.OnTrack, DeliverySignal.Blocked];
    static readonly SupportNeededSignal[] SupportOptions = [SupportNeededSignal.Low, SupportNeededSignal.Medium, SupportNeededSignal.High];

    bool _profileOpen, _signalsOpen, _focusOpen;
    string _profileName = "", _profileRole = "", _profileTz = "", _profileHours = "", _focusDraft = "";
    LoadSignal _draftLoad;
    DeliverySignal _draftDelivery;
    SupportNeededSignal _draftSupport;

    List<AzureItem> CurrentTickets =>
        Member.AzureItems.Where(a => TeamLogic.IsCurrentTicketStatus(a.Status)).ToList();

    List<AzureItem> TopCurrentTickets => CurrentTickets.Take(3).ToList();

    List<TeamNote> PinnedNotes
    {
        get
        {
            Dictionary<Guid, TeamNote> byId = Member.Notes.ToDictionary(n => n.Id);
            return Member.PinnedNoteIds
                .Select(id => byId.TryGetValue(id, out TeamNote? n) ? n : null)
                .Where(n => n is not null)
                .Cast<TeamNote>()
                .Take(3)
                .ToList();
        }
    }

    static string GetPreview(TeamNote note)
    {
        string text = System.Text.RegularExpressions.Regex.Replace(note.Text, @"\s+", " ").Trim();
        return text.Length <= 120 ? text : text[..120].Trim() + "…";
    }

    void OpenProfileModal()
    {
        _profileName = Member.Name ?? "";
        _profileRole = Member.Role ?? "";
        _profileTz = Member.Profile.TimeZone ?? "";
        _profileHours = Member.Profile.TypicalHours ?? "";
        _profileOpen = true;
    }

    void CloseProfileModal()
    {
        _profileOpen = false;
        _profileName = _profileRole = _profileTz = _profileHours = "";
    }

    async Task SaveProfile()
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

    void OpenSignalsModal()
    {
        _draftLoad = Member.Signals.Load;
        _draftDelivery = Member.Signals.Delivery;
        _draftSupport = Member.Signals.SupportNeeded;
        _signalsOpen = true;
    }

    async Task SaveSignals()
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

    void OpenFocusModal()
    {
        _focusDraft = Member.CurrentFocus ?? "";
        _focusOpen = true;
    }

    void CloseFocusModal()
    {
        _focusOpen = false;
        _focusDraft = "";
    }

    async Task SaveFocus()
    {
        TeamMember next = CloneMember(Member);
        next.CurrentFocus = _focusDraft.Trim();
        await OnUpdate.InvokeAsync(next);
        CloseFocusModal();
    }

    async Task CycleLoad()
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

    async Task CycleDelivery()
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

    async Task CycleSupport()
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

    static T Cycle<T>(T value, T[] options) where T : struct
    {
        int idx = Array.IndexOf(options, value);
        if (idx < 0) return options[0];
        return options[(idx + 1) % options.Length];
    }

    static TeamMember CloneMember(TeamMember m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Role = m.Role,
        StatusDot = m.StatusDot,
        CurrentFocus = m.CurrentFocus,
        Profile = new TeamMemberProfile { TimeZone = m.Profile.TimeZone, TypicalHours = m.Profile.TypicalHours },
        Signals = new TeamMemberSignals { Load = m.Signals.Load, Delivery = m.Signals.Delivery, SupportNeeded = m.Signals.SupportNeeded },
        Notes = m.Notes,
        PinnedNoteIds = m.PinnedNoteIds,
        ActivitySnapshot = m.ActivitySnapshot,
        AzureItems = m.AzureItems
    };
}
