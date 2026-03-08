const DEFAULT_AI_PANEL_OPEN_KEY = 'atlas.defaultAiPanelOpen'

export function loadDefaultAiPanelOpen(): boolean {
  if (typeof window === 'undefined') return false
  const raw = window.localStorage.getItem(DEFAULT_AI_PANEL_OPEN_KEY)
  if (raw == null) return false
  return raw === 'true'
}

export function saveDefaultAiPanelOpen(isOpen: boolean) {
  if (typeof window === 'undefined') return
  window.localStorage.setItem(DEFAULT_AI_PANEL_OPEN_KEY, String(isOpen))
}

