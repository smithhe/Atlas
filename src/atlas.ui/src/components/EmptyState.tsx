type EmptyStateProps = {
  icon?: string
  message: string
  detail?: string
}

export function EmptyState({ icon, message, detail }: EmptyStateProps) {
  return (
    <div className="emptyState">
      {icon ? <div className="emptyStateIcon">{icon}</div> : null}
      <div className="emptyStateMessage">{message}</div>
      {detail ? <div className="emptyStateDetail">{detail}</div> : null}
    </div>
  )
}
