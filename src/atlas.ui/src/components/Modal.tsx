import { useEffect, useId, useRef } from 'react'
import { createPortal } from 'react-dom'

export function Modal({
  title,
  isOpen,
  onClose,
  children,
  footer,
}: {
  title: string
  isOpen: boolean
  onClose: () => void
  children: React.ReactNode
  footer?: React.ReactNode
}) {
  const titleId = useId()
  const panelRef = useRef<HTMLDivElement | null>(null)
  const lastActiveRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (!isOpen) return

    lastActiveRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null
    const prevOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }

    document.addEventListener('keydown', onKeyDown)
    requestAnimationFrame(() => {
      panelRef.current?.focus()
    })

    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.body.style.overflow = prevOverflow
      lastActiveRef.current?.focus()
    }
  }, [isOpen, onClose])

  if (!isOpen) return null

  return createPortal(
    <div
      className="modalOverlay"
      role="presentation"
      onMouseDown={(e) => {
        // Click outside to close.
        if (e.target === e.currentTarget) onClose()
      }}
    >
      <div
        className="modalPanel"
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
      >
        <div className="modalHeader">
          <div className="modalTitle" id={titleId}>
            {title}
          </div>
          <button className="btn btnGhost" onClick={onClose}>
            Close
          </button>
        </div>
        <div className="modalBody">{children}</div>
        {footer ? <div className="modalFooter">{footer}</div> : null}
      </div>
    </div>,
    document.body,
  )
}



