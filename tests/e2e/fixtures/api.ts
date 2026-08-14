const apiBase = (process.env.API_BASE_URL ?? process.env.VITE_API_BASE_URL ?? 'http://localhost:5012').replace(
  /\/+$/,
  '',
)

export function getApiBaseUrl(): string {
  return apiBase
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const url = `${apiBase}${path.startsWith('/') ? path : `/${path}`}`
  const res = await fetch(url, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...(init?.headers ?? {}),
    },
  })
  if (!res.ok) {
    const body = await res.text().catch(() => '')
    throw new Error(`API ${init?.method ?? 'GET'} ${url} failed: ${res.status} ${body}`)
  }
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

export async function waitForApiHealthy(timeoutMs = 60_000): Promise<void> {
  const started = Date.now()
  let lastError: unknown
  while (Date.now() - started < timeoutMs) {
    try {
      const res = await fetch(`${apiBase}/health`)
      if (res.ok) return
      lastError = new Error(`health status ${res.status}`)
    } catch (err) {
      lastError = err
    }
    await new Promise((r) => setTimeout(r, 500))
  }
  throw new Error(`API not healthy at ${apiBase}/health: ${String(lastError)}`)
}

export async function createTeamMember(name: string): Promise<string> {
  const res = await requestJson<{ id: string }>('/team-members', {
    method: 'POST',
    body: JSON.stringify({ name, role: 'Engineer', statusDot: 'Green' }),
  })
  return res.id
}

export async function ensureTeamMember(namePrefix = 'E2E Member'): Promise<{ id: string; name: string }> {
  const list = await requestJson<Array<{ id: string; name: string }>>('/team-members')
  // Prefer demo-seeded members when present (Compose --profile demo / ATLAS_SEED_DEMO).
  const seeded = list.find((m) => /Alex|Jordan|Sam/.test(m.name))
  if (seeded) return seeded
  const existing = list.find((m) => m.name.startsWith(namePrefix))
  if (existing) return existing
  const name = `${namePrefix} ${Date.now()}`
  const id = await createTeamMember(name)
  return { id, name }
}

export async function createTaskViaApi(
  title: string,
  opts?: {
    priority?: 'Low' | 'Medium' | 'High' | 'Critical'
    status?: 'NotStarted' | 'InProgress' | 'Blocked' | 'Done'
    dueDate?: string
    notes?: string
  },
): Promise<string> {
  const res = await requestJson<{ id: string }>('/tasks', {
    method: 'POST',
    body: JSON.stringify({
      title,
      priority: opts?.priority ?? 'Medium',
      status: opts?.status ?? 'NotStarted',
      estimatedDurationText: '1h',
      estimateConfidence: 'Medium',
      notes: opts?.notes ?? '',
      dependencyTaskIds: [],
      dueDate: opts?.dueDate ?? null,
    }),
  })
  return res.id
}

export async function updateTaskViaApi(
  id: string,
  body: {
    title: string
    priority?: 'Low' | 'Medium' | 'High' | 'Critical'
    status?: 'NotStarted' | 'InProgress' | 'Blocked' | 'Done'
    notes?: string
    dueDate?: string | null
  },
): Promise<void> {
  await requestJson<void>(`/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      title: body.title,
      priority: body.priority ?? 'Medium',
      status: body.status ?? 'NotStarted',
      estimatedDurationText: '1h',
      estimateConfidence: 'Medium',
      notes: body.notes ?? '',
      dependencyTaskIds: [],
      dueDate: body.dueDate ?? null,
      assigneeId: null,
      projectId: null,
      riskId: null,
      actualDurationText: null,
    }),
  })
}

export async function createRiskViaApi(
  title: string,
  opts?: {
    status?: 'Open' | 'Watching' | 'Resolved'
    severity?: 'Low' | 'Medium' | 'High'
    description?: string
    evidence?: string
  },
): Promise<string> {
  const res = await requestJson<{ id: string }>('/risks', {
    method: 'POST',
    body: JSON.stringify({
      title,
      status: opts?.status ?? 'Open',
      severity: opts?.severity ?? 'Medium',
      projectId: null,
      description: opts?.description ?? '',
      evidence: opts?.evidence ?? '',
    }),
  })
  return res.id
}

export async function updateRiskViaApi(
  id: string,
  body: {
    title: string
    status?: 'Open' | 'Watching' | 'Resolved'
    severity?: 'Low' | 'Medium' | 'High'
    description?: string
    evidence?: string
  },
): Promise<void> {
  await requestJson<void>(`/risks/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      title: body.title,
      status: body.status ?? 'Open',
      severity: body.severity ?? 'Medium',
      projectId: null,
      description: body.description ?? '',
      evidence: body.evidence ?? '',
    }),
  })
}

export async function createProjectViaApi(
  name: string,
  opts?: { summary?: string; description?: string },
): Promise<string> {
  const res = await requestJson<{ id: string }>('/projects', {
    method: 'POST',
    body: JSON.stringify({
      name,
      summary: opts?.summary ?? 'E2E project summary',
      description: opts?.description ?? null,
      status: 'Active',
      health: 'Green',
      targetDate: null,
      priority: 'Medium',
      productOwnerId: null,
    }),
  })
  return res.id
}

export async function updateProjectViaApi(
  id: string,
  body: { name: string; summary?: string; description?: string },
): Promise<void> {
  await requestJson<void>(`/projects/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      name: body.name,
      summary: body.summary ?? 'E2E project summary',
      description: body.description ?? null,
      status: 'Active',
      health: 'Green',
      targetDate: null,
      priority: 'Medium',
      productOwnerId: null,
    }),
  })
}

export type SettingsDto = {
  staleDays: number
  defaultAiManualOnly: boolean
  theme: string
  azureDevOpsBaseUrl: string | null
}

export async function getSettingsViaApi(): Promise<SettingsDto> {
  return requestJson<SettingsDto>('/settings')
}

export async function updateSettingsViaApi(settings: SettingsDto): Promise<void> {
  await requestJson<void>('/settings', {
    method: 'PUT',
    body: JSON.stringify(settings),
  })
}

export { requestJson }
