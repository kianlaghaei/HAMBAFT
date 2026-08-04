import type { TeamExperience } from '../../api/schemas'
import { consequenceLabel, checkpointLabel } from '../../design-system/presentation'
import { EmptyState } from '../../components/States'

export function ConsequenceList({ consequences }: { consequences: NonNullable<TeamExperience['pendingConsequences']> }) {
  return consequences.length ? <ul className="ledger-list consequence-list">{consequences.map((item) => <li key={item.scheduledConsequenceId}><strong>{consequenceLabel(item.definitionId)}</strong><span>{item.dueCheckpointId ? `در ${checkpointLabel(item.dueCheckpointId)}` : 'وابسته به رویداد بازار'}</span></li>)}</ul> : <EmptyState title="پیامد آشکاری در انتظار نیست" />
}
