import type { PropsWithChildren, ReactNode } from 'react'
import { ApiError } from '../api/client'

export function LoadingState({ label = 'در حال دریافت وضعیت بازار…' }: { label?: string }) {
  return <div className="state state--loading" role="status"><span className="spinner" aria-hidden="true" />{label}</div>
}

export function EmptyState({ title, children }: PropsWithChildren<{ title: string }>) {
  return <div className="state"><h3>{title}</h3>{children && <p>{children}</p>}</div>
}

export function ErrorState({ error, retry }: { error: unknown; retry?: () => void }) {
  const message = error instanceof ApiError ? error.detail : 'خطایی پیش آمد. دوباره تلاش کنید.'
  return <div className="state state--error" role="alert"><h3>درخواست انجام نشد</h3><p>{message}</p>{retry && <button className="button button--ghost" onClick={retry}>تلاش دوباره</button>}</div>
}

export function Section({ title, eyebrow, actions, children, className = '' }: PropsWithChildren<{ title: string; eyebrow?: string; actions?: ReactNode; className?: string }>) {
  return <section className={`panel ${className}`}><header className="section-heading"><div>{eyebrow && <span className="eyebrow">{eyebrow}</span>}<h2>{title}</h2></div>{actions}</header>{children}</section>
}
