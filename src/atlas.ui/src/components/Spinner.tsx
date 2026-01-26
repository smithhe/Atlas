import type { HTMLAttributes } from 'react'

type SpinnerSize = 'sm' | 'md' | 'lg'

type SpinnerProps = {
  size?: SpinnerSize
  label?: string
  inline?: boolean
  className?: string
} & HTMLAttributes<HTMLSpanElement>

export function Spinner({
  size = 'md',
  label = 'Loading',
  inline = false,
  className,
  ...rest
}: SpinnerProps) {
  const classes = ['spinner', `spinner-${size}`]
  if (inline) classes.push('spinnerInline')
  if (className) classes.push(className)

  return <span role="status" aria-label={label} className={classes.join(' ')} {...rest} />
}
