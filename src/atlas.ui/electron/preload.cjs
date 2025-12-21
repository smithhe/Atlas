const { contextBridge } = require('electron')

// Intentionally minimal for now: keep renderer unprivileged.
contextBridge.exposeInMainWorld('atlas', {
  version: '0.0.0',
})


