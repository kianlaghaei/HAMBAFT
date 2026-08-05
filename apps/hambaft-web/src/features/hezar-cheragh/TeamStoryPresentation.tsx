export type TeamStoryPhase = 'opening' | 'investigation' | 'evidence' | 'business' | 'decision' | 'reaction'

const storyPhases: Array<{ id: TeamStoryPhase; label: string }> = [
  { id: 'opening', label: 'خبر' },
  { id: 'investigation', label: 'مکان' },
  { id: 'evidence', label: 'نشانه' },
  { id: 'business', label: 'حجره' },
  { id: 'decision', label: 'تصمیم' },
  { id: 'reaction', label: 'واکنش' },
]

export type TeamEvidenceCard = {
  kind: 'letter' | 'document' | 'seal'
  title: string
  body: string
}

export function StoryProgress({ phase }: { phase: TeamStoryPhase }) {
  const currentIndex = storyPhases.findIndex((item) => item.id === phase)
  return <ol className="hc-story-progress" aria-label="مسیر این صحنه">
    {storyPhases.map((item, index) => <li key={item.id} className={index < currentIndex ? 'is-done' : index === currentIndex ? 'is-current' : ''} aria-current={index === currentIndex ? 'step' : undefined}>{item.label}</li>)}
  </ol>
}

export function StoryOpeningCard({ title, speaker, paragraphs, instruction, onContinue }: { title: string; speaker?: string; paragraphs: string[]; instruction: string; onContinue: () => void }) {
  return <section className="hc-story-opening" data-testid="story-opening-card">
    <div className="hc-story-opening__mark" aria-hidden="true">نامه</div>
    <div className="hc-story-opening__heading"><span className="eyebrow">روایت جاری</span>{speaker && <small>{speaker}</small>}<h2>{title}</h2></div>
    <div className="hc-story-opening__copy">{paragraphs.slice(0, 3).map((paragraph) => <p key={paragraph}>{paragraph}</p>)}</div>
    <div className="hc-story-instruction"><span aria-hidden="true">۱</span><p>{instruction}</p></div>
    <button type="button" className="button button--gold" onClick={onContinue}>دیدن نشانه روی نقشه</button>
  </section>
}

export function LocationInvestigationCard({ title, identity, narrative, actionLabel, onInvestigate }: { title: string; identity: string; narrative: string; actionLabel: string; onInvestigate: () => void }) {
  return <section className="hc-investigation-scene" data-testid="location-investigation-scene">
    <div className="hc-investigation-scene__heading"><span className="hc-location-pin" aria-hidden="true" /><div><span className="eyebrow">مکان انتخاب‌شده</span><h3>{title}</h3><small>{identity}</small></div></div>
    <p>{narrative}</p>
    <div className="hc-investigation-preview"><span aria-hidden="true">مهر</span><p>این نقطه چیزی از ماجرای پاکت بی‌نام و غیبت حاج صادق را روشن می‌کند.</p></div>
    <button type="button" className="button button--gold" onClick={onInvestigate}>{actionLabel}</button>
  </section>
}

export function EvidenceReveal({ evidence, message, onContinue }: { evidence: TeamEvidenceCard[]; message: string; onContinue: () => void }) {
  return <section className="hc-evidence-reveal" data-testid="evidence-reveal">
    <div><span className="eyebrow">چیزی که پیدا کردید</span><h3>تکه‌های خبر کنار هم قرار گرفت</h3></div>
    <div className="hc-evidence-grid">{evidence.slice(0, 3).map((item) => <article className={`hc-evidence-card kind-${item.kind}`} key={`${item.kind}:${item.title}`}><span className="hc-evidence-card__object" aria-hidden="true">{item.kind === 'letter' ? 'نامه' : item.kind === 'seal' ? 'مهر' : 'سند'}</span><div><strong>{item.title}</strong><p>{item.body}</p></div></article>)}</div>
    <blockquote className="hc-clue-message"><span aria-hidden="true">«</span><p>{message}</p></blockquote>
    <button type="button" className="button button--gold" onClick={onContinue}>بردن خبر به حجره</button>
  </section>
}

export function BusinessActionCard({ businessName, summary, implication, canOfferPact, onOpenPact, onContinue }: { businessName: string; summary: string; implication: string; canOfferPact: boolean; onOpenPact: () => void; onContinue: () => void }) {
  return <section className="hc-story-business" data-testid="story-business-action">
    <div className="hc-story-business__heading"><span className="hc-business-token" aria-hidden="true">حجره</span><div><span className="eyebrow">اثر روی کسب‌وکار شما</span><h3>{businessName}</h3></div></div>
    <p>{summary}</p>
    <div className="hc-business-impact"><span aria-hidden="true">مهر</span><p><strong>چرا مهم است؟</strong> {implication}</p></div>
    {canOfferPact && <div className="hc-context-letter"><span className="hc-context-letter__seal" aria-hidden="true">✦</span><div><strong>این خبر می‌تواند موضوع یک نامه باشد</strong><p>اگر لازم است تکه دفتر را با یک حجره مقایسه کنید، پیشنهاد همکاری بفرستید.</p></div><button type="button" className="hc-inline-action" onClick={onOpenPact}>نوشتن نامه یا پیمان</button></div>}
    <button type="button" className="button button--gold" onClick={onContinue}>آماده‌کردن تصمیم نهایی</button>
  </section>
}

export function DecisionSummary({ finding, businessMeaning }: { finding: string; businessMeaning: string }) {
  return <section className="hc-final-decision" data-testid="final-decision-summary">
    <div className="hc-final-decision__heading"><span className="hc-decision-document" aria-hidden="true">جمع‌بندی</span><div><span className="eyebrow">پیش از ثبت تصمیم</span><h3>آنچه حالا می‌دانید</h3></div></div>
    <dl><div><dt>کشف شما</dt><dd>{finding}</dd></div><div><dt>اثر روی حجره</dt><dd>{businessMeaning}</dd></div></dl>
    <p className="hc-final-decision__prompt">یک مسیر را انتخاب کنید؛ نتیجه نهایی را بازار و قواعد همین پرده ثبت می‌کنند.</p>
  </section>
}

export function MarketReactionCard({ choiceLabel, reaction, waiting, onContinue }: { choiceLabel: string; reaction: string; waiting: boolean; onContinue: () => void }) {
  return <section className="hc-story-reaction" data-testid="story-market-reaction">
    <span className="hc-reaction-seal" aria-hidden="true">✦</span>
    <div><span className="eyebrow">تصمیم ثبت شد</span><h3>{choiceLabel}</h3></div>
    <p>{reaction}</p>
    {waiting && <small>واکنش کامل پس از هماهنگ‌شدن ادامه بازار نمایش داده می‌شود.</small>}
    <button type="button" className="button button--gold" onClick={onContinue}>ادامه به صحنه بعد</button>
  </section>
}
