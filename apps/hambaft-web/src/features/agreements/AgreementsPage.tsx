import { EmptyState, Section } from '../../components/States'
import { TermsView } from '../../components/TermsView'
import { agreementStatusLabel, checkpointLabel } from '../../design-system/presentation'
import { usePackage } from '../../hooks/useServerQuery'
import { useTeamContext } from '../team-experience/teamContext'

export function AgreementsPage() {
  const { experience } = useTeamContext()
  const pkg = usePackage(experience.sessionMetadata?.storyPackageId, experience.sessionMetadata?.storyVersion)
  const agreements = experience.agreements ?? []
  return <Section title="پیمان‌ها" eyebrow="تعهدهای پذیرفته‌شده">{agreements.length ? <div className="agreement-grid">{agreements.map((agreement) => <article className="agreement-card" key={agreement.agreementId}><header><h3>{pkg.data?.interactions.find((item) => item.id === agreement.interactionTypeId)?.displayName ?? 'پیمان بازار'}</h3><span className="status-chip">{agreementStatusLabel(agreement.status)}</span></header><TermsView terms={agreement.termsPayload} /><p>فعال‌شده در {checkpointLabel(agreement.activationCheckpoint)}</p><p>{agreement.executionCheckpoint ? `اجراشده در ${checkpointLabel(agreement.executionCheckpoint)}` : 'در انتظار اجرای معتبر Backend'}</p><small>{String(agreement.visibility) === 'Public' || String(agreement.visibility) === '1' ? 'پیمان عمومی' : 'خصوصی میان طرف‌ها'}</small></article>)}</div> : <EmptyState title="هنوز پیمانی شکل نگرفته است">پیشنهاد پذیرفته‌شده در این دفتر ثبت می‌شود.</EmptyState>}</Section>
}
