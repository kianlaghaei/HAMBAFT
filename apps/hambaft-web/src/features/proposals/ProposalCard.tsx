import type { Proposal, StoryPackage } from '../../api/schemas'
import { checkpointLabel, faDigits, proposalStatusLabel } from '../../design-system/presentation'
import { TermsView } from '../../components/TermsView'

export function ProposalCard({ proposal, packageData, targetName, onAction }: { proposal: Proposal; packageData?: StoryPackage; targetName: (teamId: string) => string; onAction?: (action: string, proposal: Proposal) => void }) {
  const interaction = packageData?.interactions.find((item) => item.id === proposal.interactionTypeId)
  return <article className="letter-card">
    <header><div><span className="eyebrow">{targetName(proposal.senderTeamId)} ← {targetName(proposal.receiverTeamId)}</span><h3>{interaction?.displayName ?? 'نامه بازار'}</h3></div><span className="status-chip">{proposalStatusLabel(proposal.status)}</span></header>
    <TermsView terms={proposal.termsPayload} />
    <p className="deadline">مهلت: {checkpointLabel(proposal.deadlineCheckpoint)} · بازنگری {faDigits(proposal.currentRevisionNumber)}</p>
    {(proposal.revisions?.length ?? 0) > 1 && <details><summary>تاریخچه تغییرناپذیر بازنگری‌ها</summary><div className="revision-history">{proposal.revisions?.map((revision) => <section key={revision.revisionNumber}><strong>بازنگری {faDigits(revision.revisionNumber)}</strong><TermsView terms={revision.termsPayload} /></section>)}</div></details>}
    {onAction && proposal.allowedActions.length > 0 && <footer className="button-row">{proposal.allowedActions.map((action) => <button key={action} className={action === 'Accept' ? 'button' : 'button button--ghost'} onClick={() => onAction(action, proposal)}>{({ Accept: 'پذیرش', Counter: 'پیشنهاد متقابل', Reject: 'رد', Cancel: 'لغو' } as Record<string, string>)[action] ?? action}</button>)}</footer>}
  </article>
}
