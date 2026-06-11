import './App.css'
import { RouterProvider } from 'react-router-dom'
import { appRouter } from './app/router'
import { AppProviders } from './app/providers/AppProviders'
import { AiProvider } from './app/state/AiState'

function App() {
  return (
    <AppProviders>
      <AiProvider>
        <RouterProvider router={appRouter} />
      </AiProvider>
    </AppProviders>
  )
}

export default App
