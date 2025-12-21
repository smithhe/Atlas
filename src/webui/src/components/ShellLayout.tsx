import { useEffect, useMemo, useRef, useState } from 'react'
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
  const resizeRef = useRef<{ startX: number; startWidth: number } | null>(null)

  const contextTitle = useMemo(() => routeToContextTitle(loc.pathname), [loc.pathname])

  const minAiWidth = 320
  const defaultAiWidthCss = 'clamp(320px, 26vw, 560px)'
  const aiWidthCss = ai.state.panelWidthPx ? `${Math.max(minAiWidth, ai.state.panelWidthPx)}px` : defaultAiWidthCss

  useEffect(() => {
    function onMove(e: PointerEvent) {
      if (!resizeRef.current) return
      const dx = resizeRef.current.startX - e.clientX
      const next = resizeRef.current.startWidth + dx
      const max = Math.max(minAiWidth, Math.floor(window.innerWidth * 0.6))
      ai.setPanelWidthPx(Math.max(minAiWidth, Math.min(max, Math.floor(next))))
    }
    function onUp() {
      resizeRef.current = null
    }
    window.addEventListener('pointermove', onMove)
    window.addEventListener('pointerup', onUp)
    return () => {
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerup', onUp)
    }
  }, [ai])

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

      <div
        className={`bodyGrid ${ai.state.isOpen ? 'bodyGridAiOpen' : 'bodyGridNoAi'}`}
        style={{ ['--aiWidth' as any]: aiWidthCss }}
      >
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

        {ai.state.isOpen ? (
          <div
            className="aiResizer"
            role="separator"
            aria-orientation="vertical"
            aria-label="Resize AI panel"
            tabIndex={0}
            onDoubleClick={() => ai.setPanelWidthPx(undefined)}
            onPointerDown={(e) => {
              // Start from current rendered width (fallback to min).
              const current = ai.state.panelWidthPx ?? minAiWidth
              resizeRef.current = { startX: e.clientX, startWidth: current }
              ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
            }}
            onKeyDown={(e) => {
              // Keyboard resize: arrows adjust by 20px. Home resets.
              if (e.key === 'Home') ai.setPanelWidthPx(undefined)
              if (e.key === 'ArrowLeft') ai.setPanelWidthPx((ai.state.panelWidthPx ?? minAiWidth) + 20)
              if (e.key === 'ArrowRight') ai.setPanelWidthPx((ai.state.panelWidthPx ?? minAiWidth) - 20)
            }}
          />
        ) : null}

        <AiPanel />
      </div>
    </div>
  )
}


