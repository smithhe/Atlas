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

export async function createTaskViaApi(title: string): Promise<string> {
  const res = await requestJson<{ id: string }>('/tasks', {
    method: 'POST',
    body: JSON.stringify({
      title,
      priority: 'Medium',
      status: 'NotStarted',
      estimatedDurationText: '1h',
      estimateConfidence: 'Medium',
      notes: '',
      dependencyTaskIds: [],
    }),
  })
  return res.id
}

export { requestJson }
