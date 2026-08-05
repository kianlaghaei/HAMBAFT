import { Section, EmptyState } from '../../components/States'
import { memoryLabel } from '../../design-system/presentation'
import { useTeamContext } from './teamContext'
import { ConsequenceList } from '../consequences/ConsequenceList'

export function StatusPage() {
  const { experience } = useTeamContext()
  const world = experience.worldPresentation
  const business = experience.businessPresentation
  return <div className="content-grid">
    <Section title="نبض بازار" eyebrow="معنای تألیفی وضعیت آشکار"><div className="semantic-pulse">{world?.pulse.length ? world.pulse.map((line) => <p key={line}>{line}</p>) : <p>بازار هنوز نشانه‌ی تازه‌ای آشکار نکرده است.</p>}</div></Section>
    <Section title={business ? `وضعیت ${business.displayName}` : 'وضعیت حجره'} eyebrow="پیامدهای قابل مشاهده"><div className="business-pulse">{business?.pulse.length ? business.pulse.map((line) => <p key={line}>{line}</p>) : <p>وضعیت حجره از نشانه‌های محیطی نقشه خوانده می‌شود.</p>}</div></Section>
    <Section title="رابطه‌های قابل مشاهده" eyebrow="بدون عدد و درصد">{experience.relationshipPresentation?.length ? <ul className="ledger-list">{experience.relationshipPresentation.map((relationship) => <li key={`${relationship.sourceEntityId}-${relationship.targetEntityId}-${relationship.relationshipKey}`}><strong>{relationship.label}</strong><span>{relationship.description}</span></li>)}</ul> : <EmptyState title="رابطه‌ی مرتبطی برای این حجره آشکار نیست" />}</Section>
    <Section title="یادها و نشانه‌ها" eyebrow="دفتر وقایع">{experience.visibleMemories.length ? <ul className="ledger-list">{experience.visibleMemories.map((memory) => <li key={`${memory.key}-${memory.createdAtUtc}`}>{memoryLabel(memory.key)}</li>)}</ul> : <EmptyState title="هنوز چیزی ثبت نشده است" />}</Section>
    <Section title="پیامدهای در راه" eyebrow="موعدهای قابل مشاهده"><ConsequenceList consequences={experience.pendingConsequences ?? []} /></Section>
  </div>
}
