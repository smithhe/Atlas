import type { ButtonHTMLAttributes } from 'react'
import { Spinner } from './Spinner'

type LoadingButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  loading: boolean
  spinnerLabel?: string
}

export function LoadingButton({
  loading,
  spinnerLabel,
  disabled,
  className,
  children,
  ...rest
}: LoadingButtonProps) {
  return (
    <button
      {...rest}
      className={className}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
    >
      <span className="btnContent">
        {loading ? <Spinner size="sm" inline label={spinnerLabel ?? 'Loading'} /> : null}
        <span className="btnLabel">{children}</span>
      </span>
    </button>
  )
}
