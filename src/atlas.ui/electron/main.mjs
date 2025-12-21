import { app, BrowserWindow, screen, session } from 'electron'
import fs from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

const WINDOW_STATE_FILE = 'window-state.json'

async function readWindowState() {
  try {
    const filePath = path.join(app.getPath('userData'), WINDOW_STATE_FILE)
    const raw = await fs.readFile(filePath, 'utf-8')
    const json = JSON.parse(raw)
    if (!json || typeof json !== 'object') return null
    return json
  } catch {
    return null
  }
}

async function writeWindowState(state) {
  try {
    const filePath = path.join(app.getPath('userData'), WINDOW_STATE_FILE)
    await fs.writeFile(filePath, JSON.stringify(state, null, 2), 'utf-8')
  } catch {
    // Best-effort; ignore write failures.
  }
}

function clamp(n, min, max) {
  return Math.max(min, Math.min(max, n))
}

function ensureOnSomeDisplay(bounds) {
  // If we can't determine, just accept.
  if (!bounds || typeof bounds !== 'object') return bounds
  const displays = screen.getAllDisplays()
  if (!displays || displays.length === 0) return bounds

  const x = Number(bounds.x)
  const y = Number(bounds.y)
  const w = Number(bounds.width)
  const h = Number(bounds.height)
  if (![x, y, w, h].every((v) => Number.isFinite(v))) return bounds

  const centerX = x + w / 2
  const centerY = y + h / 2
  const visible = displays.some((d) => {
    const a = d.workArea
    return centerX >= a.x && centerX <= a.x + a.width && centerY >= a.y && centerY <= a.y + a.height
  })

  if (visible) return bounds

  // Fall back to primary display work area.
  const primary = screen.getPrimaryDisplay()?.workArea ?? displays[0].workArea
  return {
    x: primary.x + 40,
    y: primary.y + 40,
    width: clamp(w, 800, primary.width),
    height: clamp(h, 600, primary.height),
  }
}

function installCsp(devUrl) {
  // IMPORTANT:
  // In dev, Vite injects inline scripts (React refresh preamble) which will be blocked unless we
  // allow unsafe-inline or manage nonces/hashes. Rather than weaken dev CSP (and fight HMR),
  // we skip CSP in dev and enforce a strict policy only when loading from file:// (built app).
  if (devUrl) return

  const devOrigin = devUrl ? new URL(devUrl).origin : null
  const devWsOrigin = devOrigin ? devOrigin.replace(/^http/, 'ws') : null

  // Dev: allow Vite/HMR. Prod: lock down to self + inline styles (we use CSS files).
  const cspDev = [
    "default-src 'self' " + devOrigin,
    "script-src 'self' 'unsafe-eval' " + devOrigin,
    "style-src 'self' 'unsafe-inline' " + devOrigin,
    "img-src 'self' data: blob: " + devOrigin,
    "font-src 'self' data:",
    "connect-src 'self' " + devOrigin + ' ' + devWsOrigin,
    "object-src 'none'",
    "base-uri 'self'",
    "frame-ancestors 'none'",
  ].join('; ')

  const cspProd = [
    "default-src 'self'",
    "script-src 'self'",
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data: blob:",
    "font-src 'self' data:",
    "connect-src 'self'",
    "object-src 'none'",
    "base-uri 'self'",
    "frame-ancestors 'none'",
  ].join('; ')

  session.defaultSession.webRequest.onHeadersReceived((details, callback) => {
    const url = details.url || ''

    // Only apply to our app entrypoints.
    const shouldApply = url.startsWith('file://')

    if (!shouldApply) {
      callback({ responseHeaders: details.responseHeaders ?? {} })
      return
    }

    const responseHeaders = details.responseHeaders ?? {}
    responseHeaders['Content-Security-Policy'] = [cspProd]
    callback({ responseHeaders })
  })
}

async function createMainWindow() {
  const devUrl = process.env.ELECTRON_RENDERER_URL
  installCsp(devUrl)

  const minWidth = 1500
  const minHeight = 820

  const saved = await readWindowState()
  const savedBounds = saved?.bounds ? ensureOnSomeDisplay(saved.bounds) : null
  const startWidth = savedBounds?.width ?? 1320
  const startHeight = savedBounds?.height ?? 840

  const win = new BrowserWindow({
    width: Math.max(minWidth, startWidth),
    height: Math.max(minHeight, startHeight),
    // Prevent shrinking into a layout where the right-side AI panel is hidden by responsive rules.
    minWidth,
    minHeight,
    x: savedBounds?.x,
    y: savedBounds?.y,
    title: 'Atlas',
    backgroundColor: '#0b0f14',
    show: false,
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  })

  win.once('ready-to-show', () => win.show())
  if (saved?.isMaximized) win.maximize()

  win.on('close', () => {
    const isMaximized = win.isMaximized()
    const bounds = isMaximized ? win.getNormalBounds() : win.getBounds()
    void writeWindowState({
      version: 1,
      isMaximized,
      bounds: {
        x: bounds.x,
        y: bounds.y,
        width: Math.max(minWidth, bounds.width),
        height: Math.max(minHeight, bounds.height),
      },
      savedAtIso: new Date().toISOString(),
    })
  })

  if (devUrl) {
    win.loadURL(devUrl)
    win.webContents.openDevTools({ mode: 'detach' })
  } else {
    // NOTE: For later packaging. For now, local dev uses Vite.
    win.loadFile(path.join(__dirname, '..', 'dist', 'index.html'))
  }
}

app.whenReady().then(() => {
  void createMainWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) void createMainWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})


