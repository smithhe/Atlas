# Blazor Phase 7 visual evidence — AI assistant

**Status: shot list only — PNGs not captured yet.** Use Playwright `ai-panel.spec.ts` + manual checklist for Phase 7 acceptance until capture runs.

Intended comparison target: fixed Phase 3 React baseline in [`../react-baseline/`](../react-baseline/) where AI shots exist.

## Expected shots (not yet committed)

| Shot | Route / state | Notes |
| --- | --- | --- |
| `30-ai-panel-closed` | `/dashboard` | Shell with AI panel closed (default); top-bar **AI ▸** visible |
| `31-ai-panel-open` | `/dashboard` + panel open | `aside[aria-label="AI panel"]`; `.aiPanelTitle` Context: Dashboard; composer + Send Prompt |
| `32-ai-panel-streaming` | panel open with stub/real stream | Transcript under `[aria-label="AI conversation"]`; Status: Completed (not Failed); Markdig turn |

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Capture

Optional follow-up: add `scripts/capture-blazor-phase7.sh` mirroring phase 4–6. Requires Atlas API on `:5012` and Blazor host on `:5173`. For streaming shots without an OpenAI key, use the Playwright AI stub (`e2e/fixtures/ai.ts`).

## Parity notes (manual)

- Panel chrome / resize / overlay: expect **match** vs React.
- Transcript markdown: expect **minor deviation** — `DisableHtml` + sanitized Markdig + highlight.js vs `react-markdown` + `rehype-highlight` (see migration handoff).
- After stubbed/real terminal SSE: status must remain **Completed** (not flipped to Failed by EventSource `onerror`).
