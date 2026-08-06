import type { TeamExperience } from '../../api/schemas'

export type TeamStoryPhase = 'opening' | 'investigation' | 'evidence' | 'business' | 'decision' | 'reaction'

export type TeamScenePresentation = NonNullable<TeamExperience['teamScenePresentation']>
export type InvestigationPresentation = TeamScenePresentation['investigationLocations'][number]
export type EvidenceItem = InvestigationPresentation['evidence'][number]
export type BusinessActivityPresentation = NonNullable<TeamScenePresentation['businessActivity']>
export type ContextualPactPresentation = TeamScenePresentation['contextualPacts'][number]
export type FinalDecisionPresentation = NonNullable<TeamScenePresentation['finalDecisionPresentation']>

export function TeamSceneUnavailableState() {
  return <section className="hc-play-screen hc-play-screen--empty" dir="rtl" data-testid="team-scene-unavailable">
    <div className="hc-empty-card">
      <span className="eyebrow">وضعیت جاری</span>
      <h1>روایت این صحنه در دسترس نیست</h1>
      <p>اطلاعات این صحنه هنوز آماده نشده است.</p>
      <p className="form-help">تا آماده‌شدن ارائه‌ی معتبر، اقدامی از این صفحه قابل ثبت نیست.</p>
    </div>
  </section>
}

export function SceneSheet({ title, subtitle, onClose, children }: { title: string; subtitle?: string; onClose: () => void; children: React.ReactNode }) {
  return <div className="hc-scene-sheet-layer" data-testid="scene-sheet-layer">
    <button type="button" className="hc-scene-sheet-dim" aria-label="بازگشت به نقشه" onClick={onClose} />
    <aside className="hc-scene-sheet" aria-label={title} data-testid="scene-sheet">
      <header className="hc-scene-sheet__header"><div><span className="eyebrow">روایت این مکان</span><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div><button type="button" className="hc-scene-sheet__close" onClick={onClose} aria-label="بستن و بازگشت به نقشه">×</button></header>
      <div className="hc-scene-sheet__body">{children}</div>
    </aside>
  </div>
}

export function LocationSceneSheet({ location, investigated, pending, onConfirm, onClose }: { location: InvestigationPresentation; investigated: boolean; pending: boolean; onConfirm: () => void; onClose: () => void }) {
  const paragraphs = [location.narrative, ...location.evidence.map((item) => item.description)].filter(Boolean).slice(0, 5)
  const primaryAction = location.evidence.flatMap((item) => item.unlocks)[0]
  return <SceneSheet title={location.title} subtitle={location.identity} onClose={onClose}>
    <div className="hc-scene-sheet__narrative">{paragraphs.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}</div>
    <div className="hc-evidence-grid">{location.evidence.map((item) => <EvidenceCard key={item.id} item={item} />)}</div>
    <section className="hc-scene-sheet__why"><span className="eyebrow">چرا این مهم است؟</span><ul>{location.evidence.map((item) => <li key={item.id}>{item.whyItMatters}</li>)}</ul></section>
    <button type="button" className="button button--gold hc-scene-sheet__action" disabled={pending} onClick={investigated ? onClose : onConfirm}>{investigated ? 'بازگشت به نقشه' : (primaryAction ?? location.title)}</button>
  </SceneSheet>
}

export function StoryOpeningCard({ title, speaker, paragraphs, instruction }: { title: string; speaker?: string; paragraphs: string[]; instruction: string }) {
  return <section className="hc-story-opening" data-testid="story-opening-card">
    <div className="hc-story-opening__mark" aria-hidden="true">روایت</div>
    <div className="hc-story-opening__heading"><span className="eyebrow">روایت جاری</span>{speaker && <small>{speaker}</small>}<h2>{title}</h2></div>
    <div className="hc-story-opening__copy">{paragraphs.slice(0, 3).map((paragraph) => <p key={paragraph}>{paragraph}</p>)}</div>
    <div className="hc-story-instruction"><span aria-hidden="true">۱</span><p>{instruction}</p></div>
  </section>
}

export function InvestigationPrompt({ objective, investigatedCount, requiredCount, locationCount }: { objective: string; investigatedCount: number; requiredCount: number; locationCount: number }) {
  const complete = investigatedCount >= requiredCount
  return <section className="hc-investigation-scene" data-testid="investigation-prompt">
    <span className="eyebrow">بررسی</span>
    <h3>{complete ? 'بررسی لازم کامل شد' : 'مکان‌های لازم را بررسی کنید'}</h3>
    <p>{objective}</p>
    <div className="hc-investigation-preview"><span aria-hidden="true">{investigatedCount}</span><p><strong>{investigatedCount} از {requiredCount} بررسی انجام شده</strong> <small>({locationCount} مکان در دسترس)</small></p></div>
  </section>
}

export function LocationInvestigationCard({ title, identity, narrative, investigatedCount, requiredCount, onInvestigate }: { title: string; identity: string; narrative: string; investigatedCount: number; requiredCount: number; onInvestigate: () => void }) {
  return <section className="hc-investigation-scene" data-testid="location-investigation-scene">
    <div className="hc-investigation-scene__heading"><span className="hc-location-pin" aria-hidden="true" /><div><span className="eyebrow">مکان انتخاب‌شده</span><h3>{title}</h3><small>{identity}</small></div></div>
    <p>{narrative}</p>
    <div className="hc-investigation-preview"><span aria-hidden="true">{investigatedCount + 1}</span><p>این بررسی شواهد ثبت‌شده‌ی همین مکان را آشکار می‌کند.</p></div>
    <button type="button" className="button button--gold" onClick={onInvestigate}>بررسی مدارک این مکان ({investigatedCount + 1} از {requiredCount})</button>
  </section>
}

const certaintyLabels = { confirmed: 'قطعی', probable: 'محتمل', uncertain: 'نامطمئن' } as const

function EvidenceCard({ item }: { item: EvidenceItem }) {
  return <article className={`hc-evidence-card certainty-${item.certainty}`} data-testid={`evidence-${item.id}`}><header><strong>{item.title}</strong><span>{certaintyLabels[item.certainty]}</span></header><small>منبع: {item.sourceLabel}</small><p>{item.description}</p><dl><div><dt>اهمیت</dt><dd>{item.whyItMatters}</dd></div>{item.unlocks.length ? <div><dt>ممکن است باز کند</dt><dd>{item.unlocks.join('، ')}</dd></div> : null}</dl></article>
}

export function EvidenceReveal({ locationName, evidence, investigatedCount, requiredCount, onContinue }: { locationName: string; evidence: EvidenceItem[]; investigatedCount: number; requiredCount: number; onContinue: () => void }) {
  const investigationComplete = investigatedCount >= requiredCount
  return <section className="hc-evidence-reveal" data-testid="evidence-reveal">
    <div><span className="eyebrow">مدارک {locationName}</span><h3>{evidence.length} نکته برای تصمیم شما</h3></div>
    <div className="hc-evidence-grid">{evidence.map((item) => <EvidenceCard key={item.id} item={item} />)}</div>
    <div className="hc-investigation-result"><strong>{investigatedCount} از {requiredCount} مکان بررسی شد.</strong><span>{investigationComplete ? 'اکنون فعالیت و تصمیم نهایی باز است.' : 'برای ادامه، یک مکان دیگر را انتخاب کنید.'}</span></div>
    <button type="button" className="button button--gold" onClick={onContinue}>{investigationComplete ? 'بردن نتیجه به حجره' : 'انتخاب مکان دوم'}</button>
  </section>
}

export function BusinessActionCard({ businessName, activity, contextualPacts, onOpenPact, onContinue, showActions = true }: { businessName: string; activity: BusinessActivityPresentation; contextualPacts: ContextualPactPresentation[]; onOpenPact: () => void; onContinue: () => void; showActions?: boolean }) {
  const contextualPact = contextualPacts[0]
  return <section className="hc-story-business" data-testid="story-business-action">
    <div className="hc-story-business__heading"><span className="hc-business-token" aria-hidden="true">حجره</span><div><span className="eyebrow">اثر روی کسب‌وکار شما</span><h3>{businessName}</h3></div></div>
    <p>{activity.summary}</p>
    <div className="hc-business-impact"><span aria-hidden="true">مهر</span><p><strong>چرا مهم است؟</strong> {activity.implication}</p></div>
    {activity.unlockedActions.length > 0 && <div className="hc-unlocked-actions"><strong>راه‌های بازشده با تحقیق شما</strong><ul>{activity.unlockedActions.map((action) => <li key={action}>{action}</li>)}</ul></div>}
    {contextualPact && <div className="hc-context-letter"><span className="hc-context-letter__seal" aria-hidden="true">✦</span><div><strong>{contextualPact.title}</strong><p>{contextualPact.message ?? contextualPact.description}</p><small>{contextualPact.unlock} · ریسک: {contextualPact.risk}</small></div>{showActions && <button type="button" className="hc-inline-action" onClick={onOpenPact}>دیدن پیمان</button>}</div>}
    {showActions && <button type="button" className="button button--gold" onClick={onContinue}>آماده‌کردن تصمیم نهایی</button>}
  </section>
}

export function DecisionSummary({ findings, investigatedLocations, businessMeaning, presentation }: { findings: string[]; investigatedLocations: string[]; businessMeaning: string; presentation?: FinalDecisionPresentation | null }) {
  const selected = presentation?.selectedChoice
  return <section className="hc-final-decision" data-testid="final-decision-summary">
    <div className="hc-final-decision__heading"><span className="hc-decision-document" aria-hidden="true">جمع‌بندی</span><div><span className="eyebrow">پیش از ثبت تصمیم</span><h3>{presentation?.heading ?? 'آنچه حالا می‌دانید'}</h3></div></div>
    {presentation?.summary && <p>{presentation.summary}</p>}
    <dl><div><dt>مکان‌های بررسی‌شده</dt><dd><ul>{investigatedLocations.map((location) => <li key={location}>{location}</li>)}</ul></dd></div><div><dt>یافته‌های شما</dt><dd><ul>{findings.map((finding) => <li key={finding}>{finding}</li>)}</ul></dd></div><div><dt>اثر روی حجره</dt><dd>{businessMeaning}</dd></div>{selected && <><div><dt>اقدام انتخاب‌شده</dt><dd>{selected.selectedAction}</dd></div><div><dt>ریسک پذیرفته‌شده</dt><dd>{selected.acceptedRisk}</dd></div><div><dt>موضع ثبت‌شده</dt><dd>{selected.position}</dd></div><div><dt>پیمان یا پیام</dt><dd>{selected.pactUsed}</dd></div></>}</dl>
    {!selected && <p className="hc-final-decision__prompt">یک مسیر را انتخاب کنید؛ نتیجه بر اساس همین یافته‌ها ثبت می‌شود.</p>}
  </section>
}

export function MarketReactionCard({ choiceLabel, reaction, additionalReactions = [], waiting, continuing = false, onContinue }: { choiceLabel: string; reaction: string; additionalReactions?: string[]; waiting: boolean; continuing?: boolean; onContinue: () => void }) {
  const reactions = [reaction, ...additionalReactions].filter(Boolean)
  return <section className="hc-story-reaction" data-testid="story-market-reaction">
    <span className="hc-reaction-seal" aria-hidden="true">✦</span>
    <div><span className="eyebrow">نتیجه تصمیم</span><h3>{choiceLabel}</h3></div>
    {waiting ? <small>واکنش کامل هنوز برای این صحنه آماده نشده است.</small> : <ul>{reactions.map((item) => <li key={item}>{item}</li>)}</ul>}
    {!continuing && <button type="button" className="button button--gold" onClick={onContinue}>ورود به صحنه بعد</button>}
  </section>
}
