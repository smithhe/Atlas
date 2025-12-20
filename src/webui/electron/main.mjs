import { app, BrowserWindow, session } from 'electron'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

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

function createMainWindow() {
  const devUrl = process.env.ELECTRON_RENDERER_URL
  installCsp(devUrl)

  const win = new BrowserWindow({
    width: 1320,
    height: 840,
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

  if (devUrl) {
    win.loadURL(devUrl)
    win.webContents.openDevTools({ mode: 'detach' })
  } else {
    // NOTE: For later packaging. For now, local dev uses Vite.
    win.loadFile(path.join(__dirname, '..', 'dist', 'index.html'))
  }
}

app.whenReady().then(() => {
  createMainWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createMainWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})


