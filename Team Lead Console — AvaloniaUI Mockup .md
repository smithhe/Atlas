# Team Lead Console — AvaloniaUI Mockup Spec (Dark Theme)

Goal: Generate AvaloniaUI **UI mockups** (Views + minimal ViewModels) for a personal desktop app used by a software team lead to manage:
1) Tasks
2) Team member performance notes + Azure DevOps work items
3) Risks & Mitigation (formerly “Problems”)

Primary requirements:
- **Dark theme everywhere**.
- **Single-window app** with left navigation, main content area, and a **collapsible AI panel** on the right.
- **Minimal, clean UI** over dense tables.
- **Keyboard-friendly** (command palette later; for now include focusable controls and sensible tab order).
- AI only acts when explicitly invoked. Outputs are drafts/suggestions; user decides.
- “Risk” terminology used globally.

---

## Global UI Shell (MainWindow)

### Layout
- Single window grid:
  - Top bar (48px)
  - Body area (remaining)
- Body area columns:
  - Left nav (220px)
  - Main content (*)
  - AI panel (320px, collapsible)

### Top Bar
- Left: App name “Team Lead Console”
- Center: Search box: “Search tasks, risks, people…”
- Right: “+ Quick Add” button
- Right: “AI ▸” toggle button (collapses/expands AI panel)

### Left Nav Buttons (vertical)
- Dashboard
- Tasks
- Team
- Risks & Mitigation
- Projects
- Settings

Navigation behavior:
- Clicking changes `CurrentView`.
- Highlight active item.
- Keep it simple (no external nav framework required).

### AI Panel (global)
- Appears only when open.
- Uses same panel across all views.
- Shows:
  - Context line (ex: “Context: Tasks” / “Context: Bob” / “Context: Risk: Refactor justification”)
  - Suggested actions relevant to the current view context
  - Output area (text) + actions: Insert Draft, Copy, Close

---

## Dark Theme Guidelines (Apply to all views)

- Backgrounds:
  - Window bg: #0F1115
  - Panels/cards: #151922 to #1A1F2B (subtle variation)
- Borders: #2A2F3A
- Text:
  - Primary: #E6EAF2
  - Secondary: #AAB2C0 (opacity ~0.8)
- Accent: blue (#3B82F6-ish) for active nav and primary buttons
- Status dots:
  - Red (critical)
  - Yellow/orange (warning)
  - Green (ok)
  - Blue (info)

Make the UI readable and consistent but not overly stylized.

---

## Dashboard View

Purpose:
- Immediate visibility into what needs attention now.
- Emphasize “Attention Required” + stale items.
- Show “Today/This Week” tasks.
- Show “Team Pulse”.

### Layout (Main content)
- Two-column in main area:
  - Left: stacked cards (Attention Required, Today/This Week, Team Pulse)
  - Right: AI panel is separate; within main content do NOT add extra AI column.

### Card 1: Attention Required
Items included:
- High-priority tasks
- Active/unresolved risks
- **Stale items**: tasks/risks with no activity for N days (default 7 or 10)
- Team members with no notes in N days

Each row includes:
- Colored dot (severity)
- Title + small metadata
- Optional type badge: Task / Risk / Team
- Click navigates to detail in relevant view

Card header includes:
- Title “Attention Required”
- Kebab menu (optional placeholder)
- Optional small “Stale threshold: 10d” indicator

### Card 2: Today / This Week
- List of top tasks (5–10)
- Show priority + estimated duration (supports days/hours)
- Button at bottom: “Ask AI: What should I work on next?”
  - Clicking should open AI panel and run “Suggest Next Action” preset

### Card 3: Team Pulse
- One line per member:
  - Name
  - Current focus snippet
  - Blocker indicator if relevant
- Click opens Team view and selects member

---

## Tasks View

Purpose:
- Your personal execution center.
- Master-detail.
- Filters for Project, Risk, Priority.
- Supports estimated duration > hours (days + hours).

### Layout
- Main content area:
  - Left (360px): filters + task list
  - Center (*): task detail editor

AI panel remains on right (global), but in mockup you may also show context actions in the AI panel when Tasks view is active.

### Left: Filters
Controls:
- Project filter (TextBox or ComboBox)
- Risk filter (TextBox or ComboBox)
- Priority filter (ComboBox: Low/Medium/High/Critical)
- Sort dropdown (optional: Priority, Due date, Last touched)

### Left: Task List
List item:
- Title
- Metadata line: Priority • Project • Risk (if present)
- Right side: estimated duration formatted (e.g., “2d”, “1d 3h”, “2h”)

Provide sample items:
- Review refactor proposal (High, 2d, Project: Core Platform, Risk: Refactor justification)
- Update onboarding docs (Medium, 1d, Project: DevEx, Risk: Onboarding drift, stale 10d)
- Review pull requests (Medium, 2h)

### Center: Task Detail
Fields:
- Title (TextBox)
- Priority (required) (ComboBox)
- Estimated Duration (required):
  - Use two inputs for mock: Days (int) + Hours (int)
  - Display computed preview “Estimated: 2d 3h”
- Project (optional) TextBox
- Risk (optional) TextBox
- Due date (optional) DatePicker
- Dependencies (optional) placeholder section “Add dependencies…”
- Notes (multi-line)

Show:
- “Last touched” timestamp
- “Stale” indicator if > threshold

Actions:
- Save (optional for mock)
- Touch/Update Activity (button)

### AI Interactions (Tasks context)
From Tasks view, AI panel should offer actions:
- Suggest Next Task
- Summarize Incomplete Work (week)
- Reprioritize suggestions (draft)

---

## Team View

Purpose:
- Track performance notes (free-form + structured).
- See current and past Azure DevOps items per member.
- Provide Azure item detail peek (ticket, PR, time taken, git history link placeholders).

### Layout
- Main content area is a 3-region layout (NOT including AI panel):
  - Left: Team member list (240px)
  - Center: Member details (notes + Azure list)
  - Optional right subpanel inside main (Azure item detail peek) OR use a collapsible panel within center
- AI panel on far right remains global and separate.

### Left: Team Member List
5 members:
- Alice
- Bob
- Charlie
- Dana
- Evan

Each member row includes:
- Avatar placeholder circle
- Name
- Small status dot
- Click selects member

### Center: Member Detail
Header:
- Name + Role/Team label (optional)
- “Current Focus” field (short text)

Section: Performance Notes
- Two modes:
  1) Quick note input (single line + Add)
  2) Structured note form (dropdown Tag + multi-line note + Add)
- Tags: Blocker, Progress, Concern, Praise, Standup
- Notes list (reverse chronological):
  - Date
  - Tag chip
  - Text
  - Optional link chips (ADO item, PR)

Section: Azure DevOps Items (Current + Past)
List shows:
- Work item ID + title
- Status (In Progress/Done/etc)
- Time taken (if known)
- Links icons: Ticket, PR, Commits (placeholders)
- Clicking selects the item and opens detail peek

### Azure Item Detail Peek (inside Team view)
Fields:
- Work item ID + title
- Status
- Assigned
- Time taken
- Links:
  - Ticket link
  - PR link
  - Git history link (placeholder)
- Notes / summary field (optional)
- Button “Open in browser”

### AI Interactions (Team context)
AI panel actions:
- Summarize patterns (frequent blockers)
- Highlight growth areas
- Cite specific notes (in output include “Based on notes: …” lines)

---

## Risks & Mitigation View

Purpose:
- Track internal/external issues with evidence.
- Link risks to tasks, team members, projects.
- Light statuses for filtering (Open/Watching/Resolved).

### Layout
- Main content: master-detail
  - Left: Risks list + filters
  - Center: Risk detail (description, evidence, links)
- AI panel on right remains global.

### Left: Risk List
List item shows:
- Title
- Status badge
- Severity dot (optional)
- “Last updated” or “Stale” indicator

Filters:
- Status
- Project
- Severity (optional)

### Center: Risk Detail
Fields:
- Title
- Status dropdown (Open/Watching/Resolved)
- Description (multi-line)
- Evidence / Examples (multi-line or list)
- Linked Tasks (list)
- Linked Team Members (list)
- Notes/History (chronological entries)

AI actions (Risk context):
- Summarize impact
- Suggest mitigations
- Explain “why this matters” (draft for justification)

---

## Projects View (Lightweight)

Purpose:
- Simple grouping for tasks/risks.
- Not intended to replace ADO/Jira.

Layout:
- Left: project list
- Center: project detail
  - Summary
  - Linked tasks
  - Linked risks
  - Team members involved

Keep minimal; provide mock data for 2–3 projects.

---

## Settings View (Minimal)

Include:
- Stale threshold (days) (default 10)
- Default AI behavior (manual only)
- Theme (locked to Dark)
- Keyboard shortcuts cheat sheet (read-only placeholder)
- External links settings placeholders (Azure DevOps base URL)

---

## Implementation Notes for Cursor (important)

- Generate **one View per tab**:
  - DashboardView
  - TasksView
  - TeamView
  - RisksView
  - ProjectsView
  - SettingsView
- For each view create a minimal ViewModel with mock sample data.
- Keep dependencies minimal:
  - No external MVVM framework required (plain INotifyPropertyChanged + basic RelayCommand).
- Wire `MainWindowViewModel.CurrentView` switching via commands when nav buttons clicked.
- AI panel should be a separate `AiPanelView` UserControl with `IsOpen` state and bindable `ContextTitle`, `Actions`, and `Output`.
- Use Avalonia standard controls; no third-party UI libs required.

Deliverables expected:
- XAML for each view + minimal ViewModels that compile.
- Use placeholders for external links (ADO/PR/Git history).
- Focus on layout and structure, not full functionality.

End of spec.
