import './App.css'
import { RouterProvider } from 'react-router-dom'
import { appRouter } from './app/router'
import { AppStateProvider } from './app/state/AppState'
import { AiProvider } from './app/state/AiState'

function App() {
  return (
    <AppStateProvider>
      <AiProvider>
        <RouterProvider router={appRouter} future={{ v7_startTransition: true } as any} />
      </AiProvider>
    </AppStateProvider>
  )
}

export default App
