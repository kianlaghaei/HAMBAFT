import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { api } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import type { Proposal, TermSchema } from '../../api/schemas'
import { useAuthStore } from '../../auth/authStore'
import { EmptyState, ErrorState, Section } from '../../components/States'
import { TermsEditor, type TermDraft } from '../../components/TermsView'
import { usePackage, usePublicWorld } from '../../hooks/useServerQuery'
import { useConnectionStatus } from '../../realtime/RealtimeBoundary'
import { useTeamContext } from '../team-experience/teamContext'
import { ProposalCard } from './ProposalCard'

function validatorFor(schema: TermSchema): z.ZodType {
  if (['Numeric', 'NumericRange'].includes(schema.type)) return z.number().min(schema.minimum ?? -Infinity).max(schema.maximum ?? Infinity)
  if (schema.type === 'Boolean') return z.boolean()
  if (['ShortText', 'ShortDeclaration', 'SingleChoice', 'TargetSelection', 'DocumentSelection', 'EvidenceSelection'].includes(schema.type)) return (schema.required ? z.string().min(1) : z.string()).max(schema.maximumLength ?? Infinity)
  if (['MultipleChoice', 'RankedChoice'].includes(schema.type)) return z.array(z.string())
  if (schema.type === 'NumericAllocation') return z.record(z.string(), z.number())
  const fields: Record<string, z.ZodType> = {}
  schema.fields.forEach((field) => { const value = validatorFor(field); fields[field.name ?? 'value'] = field.required ? value : value.optional() })
  return z.object(fields)
}

function materializeTerms(schema: TermSchema, draft: TermDraft): TermDraft {
  const result = { ...draft }
  const fields = schema.type === 'Compound' ? schema.fields : [schema]
  fields.forEach((field) => { if (field.type === 'Boolean' && field.name && result[field.name] === undefined) result[field.name] = false })
  return result
}

export function MessagesPage() {
  const { experience, canWrite } = useTeamContext()
  const token = useAuthStore((state) => state.team?.accessToken ?? '')
  const pkg = usePackage(experience.sessionMetadata?.storyPackageId, experience.sessionMetadata?.storyVersion)
  const world = usePublicWorld(experience.sessionId)
  const client = useQueryClient()
  const connection = useConnectionStatus()
  const available = experience.availableInteractionTypes ?? []
  const [interactionId, setInteractionId] = useState(available[0]?.interactionTypeId ?? '')
  const selectedAvailability = available.find((item) => item.interactionTypeId === interactionId)
  const [receiver, setReceiver] = useState(selectedAvailability?.allowedTargetTeamIds[0] ?? '')
  const [terms, setTerms] = useState<TermDraft>({})
  const [countering, setCountering] = useState<Proposal | null>(null)
  const selected = pkg.data?.interactions.find((item) => item.id === interactionId)
  const targetName = useMemo(() => (teamId: string) => world.data?.entities.find((entity) => entity.controlledByTeamId === teamId)?.displayName ?? 'حجره دیگر', [world.data])
  const mutation = useMutation({
    mutationFn: async (input: { kind: string; proposal?: Proposal }) => {
      if (input.kind === 'Send') {
        if (!selected) throw new Error('نوع تعامل در دسترس نیست.')
        const payload = materializeTerms(selected.termsSchema, terms)
        if (!validatorFor(selected.termsSchema).safeParse(payload).success) throw new Error('لطفاً همه بندها را درست کامل کنید.')
        return api.sendProposal({ interactionTypeId: interactionId, receiverTeamId: receiver, termsPayload: payload, validityType: null, validForCheckpointCount: null, validUntilCheckpointId: null, expectedStateVersion: experience.stateVersion, commandId: crypto.randomUUID() }, token)
      }
      const proposal = input.proposal
      if (!proposal) throw new Error('Proposal required')
      if (['Accept', 'Reject', 'Cancel'].includes(input.kind) && !window.confirm('این اقدام را تأیید می‌کنید؟')) throw new Error('CANCELLED')
      if (input.kind === 'Counter') {
        const interaction = pkg.data?.interactions.find((item) => item.id === proposal.interactionTypeId)
        if (!interaction) throw new Error('نوع تعامل در دسترس نیست.')
        const payload = materializeTerms(interaction.termsSchema, terms)
        if (!validatorFor(interaction.termsSchema).safeParse(payload).success) throw new Error('لطفاً همه بندها را درست کامل کنید.')
        return api.proposalAction(proposal.proposalId, 'counter', { expectedRevisionNumber: proposal.currentRevisionNumber, termsPayload: payload, expectedStateVersion: experience.stateVersion, commandId: crypto.randomUUID() }, token)
      }
      return api.proposalAction(proposal.proposalId, input.kind.toLowerCase() as 'accept' | 'reject' | 'cancel', { expectedRevisionNumber: proposal.currentRevisionNumber, expectedStateVersion: experience.stateVersion, commandId: crypto.randomUUID() }, token)
    },
    onSuccess: () => { setCountering(null); setTerms({}); void client.invalidateQueries({ queryKey: queryKeys.teamExperience }) },
  })
  function submit(event: FormEvent) { event.preventDefault(); mutation.mutate({ kind: countering ? 'Counter' : 'Send', proposal: countering ?? undefined }) }
  function action(kind: string, proposal: Proposal) {
    if (kind === 'Counter') { setCountering(proposal); setInteractionId(proposal.interactionTypeId); setTerms({ ...proposal.termsPayload } as TermDraft); return }
    mutation.mutate({ kind, proposal })
  }
  return <div className="content-grid messages-layout">
    <Section title={countering ? 'پیشنهاد متقابل' : 'نامه تازه'} eyebrow="مذاکره ساختاریافته"><form onSubmit={submit}>
      {!countering && <><label className="field"><span>نوع تعامل</span><select value={interactionId} onChange={(event) => { const id = event.target.value; setInteractionId(id); const next = available.find((item) => item.interactionTypeId === id); setReceiver(next?.allowedTargetTeamIds[0] ?? ''); setTerms({}) }}>{available.map((item) => <option key={item.interactionTypeId} value={item.interactionTypeId}>{pkg.data?.interactions.find((definition) => definition.id === item.interactionTypeId)?.displayName ?? item.interactionTypeId}</option>)}</select></label><label className="field"><span>گیرنده</span><select value={receiver} onChange={(event) => setReceiver(event.target.value)}>{selectedAvailability?.allowedTargetTeamIds.map((id) => <option value={id} key={id}>{targetName(id)}</option>)}</select></label></>}
      {selected && <><p className="form-help">{selected.description}</p><TermsEditor schema={selected.termsSchema} value={terms} onChange={setTerms} /></>}
      <div className="button-row"><button className="button" disabled={!canWrite || connection !== 'connected' || mutation.isPending || (!countering && (!interactionId || !receiver))}>{mutation.isPending ? 'در حال ثبت…' : countering ? 'ارسال بازنگری تازه' : 'ارسال پیشنهاد'}</button>{countering && <button type="button" className="button button--ghost" onClick={() => { setCountering(null); setTerms({}) }}>انصراف</button>}</div>
      {mutation.isError && mutation.error.message !== 'CANCELLED' && <ErrorState error={mutation.error} />}
    </form></Section>
    <Section title="صندوق ورودی" eyebrow="نامه‌های رسیده">{experience.inbox?.length ? <div className="letter-stack">{experience.inbox.map((proposal) => <ProposalCard key={proposal.proposalId} proposal={proposal} packageData={pkg.data} targetName={targetName} onAction={action} />)}</div> : <EmptyState title="نامه تازه‌ای نیست">ورودی شما با رویدادهای بازار خودکار تازه می‌شود.</EmptyState>}</Section>
    <Section title="صندوق خروجی" eyebrow="پیشنهادهای فرستاده‌شده">{experience.outbox?.length ? <div className="letter-stack">{experience.outbox.map((proposal) => <ProposalCard key={proposal.proposalId} proposal={proposal} packageData={pkg.data} targetName={targetName} onAction={action} />)}</div> : <EmptyState title="هنوز پیشنهادی نفرستاده‌اید" />}</Section>
  </div>
}
