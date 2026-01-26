import type { ReactNode } from 'react'
import { Spinner } from './Spinner'

type LoadingOverlayProps = {
  isLoading: boolean
  label?: string
  spinnerSize?: 'sm' | 'md' | 'lg'
  className?: string
  children: ReactNode
}

export function LoadingOverlay({
  isLoading,
  label = 'Loading',
  spinnerSize = 'md',
  className,
  children,
}: LoadingOverlayProps) {
  const classes = ['loadingOverlayWrap']
  if (isLoading) classes.push('loadingOverlayWrapActive')
  if (className) classes.push(className)

  return (
    <div className={classes.join(' ')}>
      <div className="loadingOverlayBody" aria-hidden={isLoading || undefined}>
        {children}
      </div>
      {isLoading ? (
        <div className="loadingOverlay" role="presentation">
          <Spinner size={spinnerSize} label={label} />
        </div>
      ) : null}
    </div>
  )
}
