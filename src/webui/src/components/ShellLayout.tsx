import { useMemo, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { AiPanel } from './AiPanel'
import { useAi } from '../app/state/AiState'

type NavItem = { to: string; label: string }

const NAV: NavItem[] = [
  { to: '/', label: 'Dashboard' },
  { to: '/tasks', label: 'Tasks' },
  { to: '/team', label: 'Team' },
  { to: '/risks', label: 'Risks & Mitigation' },
  { to: '/projects', label: 'Projects' },
  { to: '/settings', label: 'Settings' },
]

function routeToContextTitle(pathname: string) {
  if (pathname.startsWith('/tasks')) return 'Context: Tasks'
  if (pathname.startsWith('/team')) return 'Context: Team'
  if (pathname.startsWith('/risks')) return 'Context: Risks'
  if (pathname.startsWith('/projects')) return 'Context: Projects'
  if (pathname.startsWith('/settings')) return 'Context: Settings'
  return 'Context: Dashboard'
}

export function ShellLayout() {
  const ai = useAi()
  const loc = useLocation()
  const [search, setSearch] = useState('')

  const contextTitle = useMemo(() => routeToContextTitle(loc.pathname), [loc.pathname])

  return (
    <div className="appShell">
      <header className="topBar">
        <div className="topBarLeft">
          <div className="appTitle">Atlas</div>
        </div>
        <div className="topBarCenter">
          <input
            className="searchInput"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search tasks, risks, people…"
            aria-label="Search"
          />
        </div>
        <div className="topBarRight">
          <button className="btn btnSecondary" onClick={() => ai.appendOutput('\n(+ Quick Add: placeholder)\n')}>
            + Quick Add
          </button>
          <button className="btn" onClick={() => ai.setIsOpen(!ai.state.isOpen)}>
            AI ▸
          </button>
        </div>
      </header>

      <div className={`bodyGrid ${ai.state.isOpen ? '' : 'bodyGridNoAi'}`}>
        <nav className="leftNav" aria-label="Primary navigation">
          {NAV.map((n) => (
            <NavLink
              key={n.to}
              to={n.to}
              end={n.to === '/'}
              className={({ isActive }) => `navBtn ${isActive ? 'navBtnActive' : ''}`}
            >
              {n.label}
            </NavLink>
          ))}
        </nav>

        <main className="mainContent" aria-label="Main content">
          {/* Keep AI context in sync with route even if a view forgets to set it */}
          <div className="srOnly" aria-hidden="true">
            {contextTitle}
          </div>
          <Outlet />
        </main>

        <AiPanel />
      </div>
    </div>
  )
}


