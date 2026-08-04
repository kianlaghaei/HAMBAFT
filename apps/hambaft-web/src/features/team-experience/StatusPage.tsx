import { Section, EmptyState } from '../../components/States'
import { faDigits, memoryLabel, metricLabel } from '../../design-system/presentation'
import { useTeamContext } from './teamContext'
import { ConsequenceList } from '../consequences/ConsequenceList'

export function MetricCard({ label, value, min = 0, max = 100 }: { label: string; value: number; min?: number; max?: number }) {
  const percent = Math.max(0, Math.min(100, ((value - min) / Math.max(1, max - min)) * 100))
  return <article className="metric-card"><div><span>{label}</span><strong>{faDigits(value)}</strong></div><div className="meter" role="meter" aria-label={label} aria-valuenow={value} aria-valuemin={min} aria-valuemax={max}><span style={{ width: `${percent}%` }} /></div></article>
}

export function StatusPage() {
  const { experience } = useTeamContext()
  return <div className="content-grid">
    <Section title="دفتر وضعیت" eyebrow="آنچه برای حجره شما آشکار است"><div className="metrics-grid">{experience.visibleMetrics.map((metric) => <MetricCard key={`${metric.scope}-${metric.metricKey}`} label={metricLabel(metric.metricKey)} value={metric.numericValue} />)}</div></Section>
    <Section title="یادها و نشانه‌ها" eyebrow="دفتر وقایع">{experience.visibleMemories.length ? <ul className="ledger-list">{experience.visibleMemories.map((memory) => <li key={`${memory.key}-${memory.createdAtUtc}`}>{memoryLabel(memory.key)}</li>)}</ul> : <EmptyState title="هنوز چیزی ثبت نشده است" />}</Section>
    <Section title="پیامدهای در راه" eyebrow="موعدهای قابل مشاهده"><ConsequenceList consequences={experience.pendingConsequences ?? []} /></Section>
  </div>
}
