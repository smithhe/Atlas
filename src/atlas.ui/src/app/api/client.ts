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

export function apiBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_API_BASE_URL
  if (fromEnv?.trim()) {
    return fromEnv.replace(/\/+$/, '')
  }

  // Local dev keeps the API on a separate port; deployed web defaults to same-origin.
  const hostname = window.location.hostname
  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    return 'http://localhost:5012'
  }

  return window.location.origin.replace(/\/+$/, '')
}

export function toApiUrl(path: string): string {
  return `${apiBaseUrl()}${path.startsWith('/') ? '' : '/'}${path}`
}

export async function getJson<T>(path: string, init?: RequestInit): Promise<T> {
  const url = toApiUrl(path)
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

async function sendJson<T>(path: string, method: 'POST' | 'PUT', body: unknown, init?: RequestInit): Promise<T> {
  const url = toApiUrl(path)
  const res = await fetch(url, {
    ...init,
    method,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    body: JSON.stringify(body),
  })

  if (!res.ok) {
    throw new HttpError({ status: res.status, url })
  }

  if (res.status === 204) {
    return undefined as unknown as T
  }

  return (await res.json()) as T
}

export function postJson<T>(path: string, body: unknown, init?: RequestInit): Promise<T> {
  return sendJson<T>(path, 'POST', body, init)
}

export function putJson<T>(path: string, body: unknown, init?: RequestInit): Promise<T> {
  return sendJson<T>(path, 'PUT', body, init)
}

export async function deleteJson(path: string, init?: RequestInit): Promise<void> {
  const url = toApiUrl(path)
  const res = await fetch(url, {
    ...init,
    method: 'DELETE',
    headers: {
      Accept: 'application/json',
      ...(init?.headers ?? {}),
    },
  })

  if (!res.ok) {
    throw new HttpError({ status: res.status, url })
  }
}

