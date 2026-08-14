import type { Page, Route } from '@playwright/test'
import { getApiBaseUrl } from './api'

const STUB_DRAFT = 'Playwright stub draft for Insert Draft.'

type StubOptions = {
  assistantText?: string
}

function ssePayload(eventType: string, data: Record<string, unknown>): string {
  return `event: ${eventType}\ndata: ${JSON.stringify(data)}\n\n`
}

function buildSseBody(sessionId: string, text: string): string {
  const now = new Date().toISOString()
  return [
    ssePayload('session.started', {
      eventId: crypto.randomUUID(),
      sessionId,
      sequence: 1,
      type: 'session.started',
      status: 'started',
      message: 'Session started.',
      occurredAtUtc: now,
      isTerminal: false,
    }),
    ssePayload('model.delta', {
      eventId: crypto.randomUUID(),
      sessionId,
      sequence: 2,
      type: 'model.delta',
      status: 'streaming',
      delta: text,
      occurredAtUtc: now,
      isTerminal: false,
    }),
    ssePayload('session.completed', {
      eventId: crypto.randomUUID(),
      sessionId,
      sequence: 3,
      type: 'session.completed',
      status: 'completed',
      message: 'Completed.',
      occurredAtUtc: now,
      isTerminal: true,
    }),
  ].join('')
}

/**
 * Stub AI conversation + SSE endpoints so Insert Draft / conversation start
 * work without a real OpenAI key. CRUD and missing-key flows should not call this.
 *
 * EventSource auto-reconnects when a stream ends; we answer with the same
 * terminal payload so the UI settles on the stubbed assistant text.
 */
export async function stubAiConversation(page: Page, options: StubOptions = {}): Promise<void> {
  const assistantText = options.assistantText ?? STUB_DRAFT
  const apiBase = getApiBaseUrl()

  await page.route((url) => {
    const href = typeof url === 'string' ? url : url.href
    return href.startsWith(`${apiBase}/ai/`)
  }, async (route: Route) => {
    const request = route.request()
    const method = request.method()
    const url = request.url()
    const path = new URL(url).pathname

    if (path === '/ai/conversations' && method === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
      return
    }

    if (path === '/ai/conversations' && method === 'POST') {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({
          conversationId: crypto.randomUUID(),
          turnSessionId: crypto.randomUUID(),
        }),
      })
      return
    }

    if (/^\/ai\/conversations\/[^/]+\/messages$/.test(path) && method === 'POST') {
      await route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({ turnSessionId: crypto.randomUUID() }),
      })
      return
    }

    if (/^\/ai\/conversations\/[^/]+$/.test(path) && method === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          conversationId: path.split('/').pop(),
          title: 'Stub conversation',
          view: 'Tasks',
          createdAtUtc: new Date().toISOString(),
          updatedAtUtc: new Date().toISOString(),
          turns: [],
        }),
      })
      return
    }

    if (/^\/ai\/sessions\/[^/]+\/events$/.test(path) && method === 'GET') {
      const sessionId = path.split('/')[3] ?? crypto.randomUUID()
      await route.fulfill({
        status: 200,
        headers: {
          'content-type': 'text/event-stream; charset=utf-8',
          'cache-control': 'no-cache',
          connection: 'keep-alive',
        },
        body: buildSseBody(sessionId, assistantText),
      })
      return
    }

    await route.continue()
  })
}

export { STUB_DRAFT }
