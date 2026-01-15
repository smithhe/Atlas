import type { Settings } from '../types'
import { putJson } from './client'

export function updateSettings(settings: Settings): Promise<void> {
  return putJson<void>('/settings', {
    staleDays: settings.staleDays,
    defaultAiManualOnly: settings.defaultAiManualOnly,
    theme: settings.theme,
    azureDevOpsBaseUrl: settings.azureDevOpsBaseUrl ?? null,
  })
}
