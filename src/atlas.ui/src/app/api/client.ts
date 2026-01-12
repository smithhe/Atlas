export class HttpError extends Error {
  readonly status: number
  readonly url: string

  constructor(params: { status: number; url: string; message?: string }) {
    super(params.message ?? `HTTP ${params.status} for ${params.url}`)
    this.name = 'HttpError'
    this.status = params.status
    this.url = params.url
  }
}

function apiBaseUrl(): string {
  const fromEnv = (import.meta as any).env?.VITE_API_BASE_URL as string | undefined
  return (fromEnv ?? 'http://localhost:5012').replace(/\/+$/, '')
}

export async function getJson<T>(path: string, init?: RequestInit): Promise<T> {
  const url = `${apiBaseUrl()}${path.startsWith('/') ? '' : '/'}${path}`
  const res = await fetch(url, {
    ...init,
    method: 'GET',
    headers: {
      Accept: 'application/json',
      ...(init?.headers ?? {}),
    },
  })

  if (!res.ok) {
    throw new HttpError({ status: res.status, url })
  }

  return (await res.json()) as T
}

