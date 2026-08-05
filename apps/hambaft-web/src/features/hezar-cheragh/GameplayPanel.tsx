import { type ReactNode } from 'react'
import type { PanelFixture, PanelMode, BusinessActivityKind, InvestigationFixture, BusinessActivityFixture, PactTargetFixture, ProposalLetterFixture, CounterproposalFixture, WorldReactionFixture } from './fixtures'

export type GameplayPanelProps = {
  fixture: PanelFixture
  currentReactionIndex?: number
  totalReactions?: number
  onClose?: () => void
  onConfirm?: () => void
  onNextReaction?: () => void
  onSkipReactions?: () => void
  onSelectPactTarget?: (id: string) => void
  onAcceptProposal?: () => void
  onRejectProposal?: () => void
  onCounterProposal?: () => void
  onInspectLocation?: (id: string) => void
}

export function GameplayPanel({
  fixture,
  currentReactionIndex = 0,
  totalReactions = 0,
  onClose,
  onConfirm,
  onNextReaction,
  onSkipReactions,
  onSelectPactTarget,
  onAcceptProposal,
  onRejectProposal,
  onCounterProposal,
  onInspectLocation,
}: GameplayPanelProps) {
  const mode = fixture.mode
  return (
    <aside className="gameplay-panel" data-testid="gameplay-panel" data-mode={mode} role="complementary" aria-label="پنل بازی">
      <PanelHeader mode={mode} onClose={onClose} />
      <div className="panel-body">
        {renderMode(fixture, {
          currentReactionIndex,
          totalReactions,
          onConfirm,
          onNextReaction,
          onSkipReactions,
          onSelectPactTarget,
          onAcceptProposal,
          onRejectProposal,
          onCounterProposal,
          onInspectLocation,
        })}
      </div>
    </aside>
  )
}

function PanelHeader({ mode, onClose }: { mode: PanelMode; onClose?: () => void }) {
  const labels: Record<PanelMode, string> = {
    SceneIntroduction: 'شروع روایت',
    LocationContext: 'اطلاعات مکان',
    Investigation: 'بررسی',
    BusinessActivity: 'فعالیت',
    PactTargetSelection: 'انتخاب هدف پیمان',
    ProposalLetter: 'نامه پیشنهاد',
    Counterproposal: 'پیشنهاد متقابل',
    CompositeCommitment: 'پیمان‌های فعال',
    WaitingForOtherTeams: 'در انتظار',
    WorldReaction: 'واکنش بازار',
    EntityEnding: 'پایان مسیر',
  }
  return (
    <div className="panel-header">
      <span className="panel-mode-label">{labels[mode]}</span>
      {onClose && (
        <button type="button" className="panel-close" onClick={onClose} aria-label="بستن">
          ✕
        </button>
      )}
    </div>
  )
}

type RenderContext = {
  currentReactionIndex: number
  totalReactions: number
  onConfirm?: () => void
  onNextReaction?: () => void
  onSkipReactions?: () => void
  onSelectPactTarget?: (id: string) => void
  onAcceptProposal?: () => void
  onRejectProposal?: () => void
  onCounterProposal?: () => void
  onInspectLocation?: (id: string) => void
}

function renderMode(fixture: PanelFixture, ctx: RenderContext): ReactNode {
  switch (fixture.mode) {
    case 'SceneIntroduction':
      return <SceneIntroductionView fixture={fixture} onConfirm={ctx.onConfirm} />
    case 'LocationContext':
      return <LocationContextView fixture={fixture} onInspectLocation={ctx.onInspectLocation} />
    case 'Investigation':
      return <InvestigationView fixture={fixture} onInspectLocation={ctx.onInspectLocation} />
    case 'BusinessActivity':
      return <BusinessActivityView fixture={fixture} onConfirm={ctx.onConfirm} />
    case 'PactTargetSelection':
      return <PactTargetSelectionView fixture={fixture} onSelectPactTarget={ctx.onSelectPactTarget} />
    case 'ProposalLetter':
      return <ProposalLetterView fixture={fixture} onAccept={ctx.onAcceptProposal} onReject={ctx.onRejectProposal} onCounter={ctx.onCounterProposal} />
    case 'Counterproposal':
      return <CounterproposalView fixture={fixture} onAccept={ctx.onAcceptProposal} onReject={ctx.onRejectProposal} onCounter={ctx.onCounterProposal} />
    case 'CompositeCommitment':
      return <CompositeCommitmentView fixture={fixture} />
    case 'WaitingForOtherTeams':
      return <WaitingView fixture={fixture} />
    case 'WorldReaction':
      return <WorldReactionView fixture={fixture} currentIndex={ctx.currentReactionIndex} total={ctx.totalReactions} onNext={ctx.onNextReaction} onSkip={ctx.onSkipReactions} />
    case 'EntityEnding':
      return <EntityEndingView fixture={fixture} />
    default:
      return <p>حالت ناشناخته</p>
  }
}

/* ─── Mode renderers ─── */

function SceneIntroductionView({ fixture, onConfirm }: { fixture: { mode: 'SceneIntroduction'; title: string; narrative: string; eventTitle: string }; onConfirm?: () => void }) {
  return (
    <div className="panel-mode scene-introduction">
      <h2 className="panel-event-title">{fixture.eventTitle}</h2>
      <h3 className="panel-title">{fixture.title}</h3>
      <p className="panel-narrative">{fixture.narrative}</p>
      <div className="panel-character-area">
        <div className="character-silhouette" aria-hidden="true" />
        <span className="character-hint">راوی بازار</span>
      </div>
      {onConfirm && (
        <button type="button" className="button button--gold panel-primary-action" onClick={onConfirm}>
          ادامه
        </button>
      )}
    </div>
  )
}

function LocationContextView({ fixture, onInspectLocation }: { fixture: { mode: 'LocationContext'; location: { name: string; whyItMatters: string; whoIsHere: string; currentCondition: string; recentChange: string; availableActions: string[] } }; onInspectLocation?: (id: string) => void }) {
  const loc = fixture.location
  return (
    <div className="panel-mode location-context">
      <h2 className="panel-event-title">{loc.name}</h2>
      <p className="eyebrow">چرا مهم است؟</p>
      <p>{loc.whyItMatters}</p>
      <p className="eyebrow">اکنون</p>
      <p className="panel-condition">{loc.currentCondition}</p>
      {loc.whoIsHere && <><p className="eyebrow">حاضر</p><p>{loc.whoIsHere}</p></>}
      {loc.recentChange && <><p className="eyebrow">تغییر اخیر</p><p className="panel-change">{loc.recentChange}</p></>}
      {loc.availableActions.length > 0 && (
        <div className="panel-actions">
          {loc.availableActions.map((action) => (
            <button key={action} type="button" className="button panel-action" onClick={() => onInspectLocation?.(action)}>
              {action}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

function InvestigationView({ fixture, onInspectLocation }: { fixture: InvestigationFixture; onInspectLocation?: (id: string) => void }) {
  const seals: boolean[] = []
  for (let i = 0; i < fixture.totalSeals; i++) seals.push(i < fixture.remainingSeals)

  return (
    <div className="panel-mode investigation">
      <h2 className="panel-event-title">بررسی بازار</h2>
      <div className="investigation-seals" aria-label={`${fixture.remainingSeals} مهر بررسی در اختیار دارید`}>
        {seals.map((available, i) => (
          <span
            key={i}
            className={`investigation-seal ${available ? 'is-available' : 'is-spent'}`}
            aria-hidden="true"
          >
            {available ? '✦' : '✧'}
          </span>
        ))}
      </div>
      {fixture.remainingSeals > 0 ? (
        <p className="investigation-readiness">
          یک مهر بررسی دیگر در اختیار دارید.
        </p>
      ) : (
        <p className="investigation-readiness is-exhausted">
          همه مهرهای بررسی مصرف شده‌اند.
        </p>
      )}
      <div className="investigation-locations">
        <p className="eyebrow">مکان‌های قابل بررسی</p>
        <ul>
          {fixture.eligibleLocations.map((loc) => (
            <li key={loc.id}>
              <button
                type="button"
                className="button button--ghost"
                disabled={fixture.remainingSeals === 0}
                onClick={() => onInspectLocation?.(loc.id)}
              >
                {loc.name}
              </button>
              <span className="location-condition">{loc.condition}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

function BusinessActivityView({ fixture, onConfirm }: { fixture: BusinessActivityFixture; onConfirm?: () => void }) {
  return (
    <div className="panel-mode business-activity" data-activity={fixture.kind}>
      <h2 className="panel-event-title">{fixture.title}</h2>
      <p className="panel-instructions">{fixture.instructions}</p>
      <div className="activity-visual" aria-hidden="true">
        <ActivityPlaceholder kind={fixture.kind} />
      </div>
      <p className="activity-implication">{fixture.implication}</p>
      {fixture.canConfirm && (
        <button type="button" className="button button--gold panel-primary-action" onClick={onConfirm}>
          تأیید
        </button>
      )}
      {!fixture.canConfirm && (
        <p className="activity-waiting" role="status">منتظر شرایط مناسب...</p>
      )}
    </div>
  )
}

function ActivityPlaceholder({ kind }: { kind: BusinessActivityKind }) {
  const visuals: Record<BusinessActivityKind, { shape: string; label: string }> = {
    BakerySupplyAllocation: { shape: 'M10 50 L50 10 L90 50 Z', label: 'نانوایی' },
    LogisticsRouteBoard: { shape: 'M10 10 H90 V30 H70 V50 H90 V70 H10 V50 H30 V30 H10 Z', label: 'باربری' },
    PrintingEvidenceDesk: { shape: 'M10 10 H90 V70 H10 Z M30 30 H70 V50 H30 Z', label: 'چاپ' },
    ExchangeGuaranteeBoard: { shape: 'M50 10 L90 40 L50 70 L10 40 Z', label: 'صرافی' },
  }
  const visual = visuals[kind]
  return (
    <svg className="activity-placeholder" viewBox="0 0 100 80" aria-label={visual.label}>
      <rect width="100" height="80" fill="var(--paper-deep)" rx="4" />
      <path d={visual.shape} fill="var(--gold-light)" stroke="var(--gold)" strokeWidth="2" />
      <text x="50" y="75" textAnchor="middle" fontSize="8" fill="var(--ink)">{visual.label}</text>
    </svg>
  )
}

function PactTargetSelectionView({ fixture, onSelectPactTarget }: { fixture: PactTargetFixture; onSelectPactTarget?: (id: string) => void }) {
  return (
    <div className="panel-mode pact-target-selection">
      <h2 className="panel-event-title">انتخاب طرف پیمان</h2>
      <p className="panel-instructions">حجره‌ای که می‌خواهید با آن پیمان ببندید را انتخاب کنید.</p>
      <ul className="pact-target-list">
        {fixture.availableTargets.map((t) => (
          <li key={t.id}>
            <button
              type="button"
              className={`pact-target-option${fixture.selectedTargetId === t.id ? ' is-selected' : ''}`}
              onClick={() => onSelectPactTarget?.(t.id)}
            >
              <span className="target-name">{t.name}</span>
              <span className="target-kind">{t.businessKind}</span>
              <span className="target-condition">{t.condition}</span>
            </button>
          </li>
        ))}
      </ul>
    </div>
  )
}

function ProposalLetterView({
  fixture,
  onAccept,
  onReject,
  onCounter,
}: {
  fixture: ProposalLetterFixture
  onAccept?: () => void
  onReject?: () => void
  onCounter?: () => void
}) {
  return (
    <div className="panel-mode proposal-letter">
      <div className="letter-paper">
        <h2 className="letter-title">{fixture.title}</h2>
        <div className="letter-parties">
          <span>از: <strong>{fixture.from}</strong></span>
          <span>به: <strong>{fixture.to}</strong></span>
        </div>
        <p className="letter-body">{fixture.body}</p>
        <div className="letter-terms">
          <p className="eyebrow">شرایط پیشنهادی</p>
          <dl>
            {fixture.terms.map((term, i) => (
              <div key={i} className="letter-term">
                <dt>{term.name}</dt>
                <dd>{term.value}</dd>
              </div>
            ))}
          </dl>
        </div>
      </div>
      <div className="button-row">
        <button type="button" className="button button--gold" onClick={onAccept}>پذیرش</button>
        <button type="button" className="button" onClick={onCounter}>پیشنهاد متقابل</button>
        <button type="button" className="button button--ghost" onClick={onReject}>رد</button>
      </div>
    </div>
  )
}

function CounterproposalView({
  fixture,
  onAccept,
  onReject,
  onCounter,
}: {
  fixture: CounterproposalFixture
  onAccept?: () => void
  onReject?: () => void
  onCounter?: () => void
}) {
  return (
    <div className="panel-mode counterproposal">
      <div className="letter-paper">
        <h2 className="letter-title">{fixture.title}</h2>
        <div className="letter-parties">
          <span>از: <strong>{fixture.from}</strong></span>
          <span>به: <strong>{fixture.to}</strong></span>
        </div>
        <div className="letter-terms">
          <p className="eyebrow">شرایط بازبینی‌شده</p>
          <dl>
            {fixture.originalTerms.map((term, i) => (
              <div key={`orig-${i}`} className={`letter-term term-${term.status}`}>
                <dt>
                  {term.status === 'removed' && <span className="term-removed-mark" aria-label="حذف‌شده">−</span>}
                  {term.status === 'changed' && <span className="term-changed-mark" aria-label="تغییریافته">~</span>}
                  {term.name}
                </dt>
                <dd className={term.status === 'removed' ? 'is-removed' : ''}>{term.value}</dd>
              </div>
            ))}
            {fixture.addedTerms.map((term, i) => (
              <div key={`add-${i}`} className="letter-term term-added">
                <dt>
                  <span className="term-added-mark" aria-label="اضافه‌شده">+</span>
                  {term.name}
                </dt>
                <dd>{term.value}</dd>
              </div>
            ))}
          </dl>
        </div>
      </div>
      <div className="button-row">
        <button type="button" className="button button--gold" onClick={onAccept}>پذیرش</button>
        <button type="button" className="button" onClick={onCounter}>ویرایش بیشتر</button>
        <button type="button" className="button button--ghost" onClick={onReject}>رد</button>
      </div>
    </div>
  )
}

function CompositeCommitmentView({ fixture }: { fixture: { mode: 'CompositeCommitment'; agreements: Array<{ title: string; parties: string[]; description: string }> } }) {
  return (
    <div className="panel-mode composite-commitment">
      {fixture.agreements.map((agreement, i) => (
        <div key={i} className="commitment-card">
          <h3>{agreement.title}</h3>
          <p className="commitment-parties">{agreement.parties.join(' • ')}</p>
          <p>{agreement.description}</p>
        </div>
      ))}
    </div>
  )
}

function WaitingView({ fixture }: { fixture: { mode: 'WaitingForOtherTeams'; message: string } }) {
  return (
    <div className="panel-mode waiting">
      <div className="waiting-indicator" aria-hidden="true">
        <span className="waiting-dot" />
        <span className="waiting-dot" />
        <span className="waiting-dot" />
      </div>
      <p className="panel-narrative">{fixture.message}</p>
    </div>
  )
}

function WorldReactionView({
  fixture,
  currentIndex,
  total,
  onNext,
  onSkip,
}: {
  fixture: WorldReactionFixture
  currentIndex: number
  total: number
  onNext?: () => void
  onSkip?: () => void
}) {
  if (!fixture.reactions[currentIndex]) return null
  const reaction = fixture.reactions[currentIndex]

  return (
    <div className="panel-mode world-reaction">
      <p className="reaction-progress">{currentIndex + 1} از {total}</p>
      <div className="reaction-card">
        <span className="reaction-location">{reaction.locationName}</span>
        <p className="reaction-outcome">{reaction.outcomeLine}</p>
      </div>
      <div className="reaction-actions">
        {currentIndex < total - 1 && (
          <button type="button" className="button" onClick={onNext}>بعدی</button>
        )}
        <button type="button" className="button button--ghost" onClick={onSkip}>رد کردن</button>
        {currentIndex === total - 1 && (
          <button type="button" className="button button--gold" onClick={onNext}>ادامه روایت</button>
        )}
      </div>
    </div>
  )
}

function EntityEndingView({ fixture }: { fixture: { mode: 'EntityEnding'; title: string; evidence: string[]; narrative: string } }) {
  return (
    <div className="panel-mode entity-ending">
      <h2 className="panel-event-title ending-title">{fixture.title}</h2>
      <p className="panel-narrative">{fixture.narrative}</p>
      {fixture.evidence.length > 0 && (
        <div className="ending-evidence">
          <p className="eyebrow">مستندات</p>
          <ul>
            {fixture.evidence.map((e, i) => (
              <li key={i} className="evidence-item">{e}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
