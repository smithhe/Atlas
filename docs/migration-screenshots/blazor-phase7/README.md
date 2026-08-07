# Blazor Phase 7 visual evidence — AI assistant

Screenshots from the **Blazor** UI (`src/frontend/Atlas.Ui`) after Phase 7 AI panel parity, compared against the fixed Phase 3 React baseline in [`../react-baseline/`](../react-baseline/) where AI shots exist.

## Expected shots

| Shot | Route / state | Notes |
| --- | --- | --- |
| `30-ai-panel-closed` | `/dashboard` | Shell with AI panel closed (default); top-bar **AI ▸** visible |
| `31-ai-panel-open` | `/dashboard` + panel open | `aside[aria-label="AI panel"]`; `.aiPanelTitle` Context: Dashboard; composer + Send Prompt |
| `32-ai-panel-streaming` | panel open with stub/real stream | Transcript under `[aria-label="AI conversation"]`; markdown assistant turn (Markdig) |

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Capture

Optional (timeboxed): add `scripts/capture-blazor-phase7.sh` mirroring phase 4–6 capture scripts. Until captured, leave this folder as the documented evidence path for PR handoff.

Requires Atlas API on `:5012` and Blazor host on `:5173`. For streaming shots without an OpenAI key, use the Playwright AI stub (`e2e/fixtures/ai.ts`) or a configured `OpenAI__ApiKey`.

## Parity notes (manual)

- Panel chrome / resize / overlay: expect **match** vs React.
- Transcript markdown: expect **minor deviation** — sanitized Markdig + highlight.js vs `react-markdown` + `rehype-highlight` (see migration handoff).
