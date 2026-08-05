export type TeamStoryPhase = 'opening' | 'investigation' | 'evidence' | 'business' | 'decision' | 'reaction'

export type EvidenceItem = {
  id: string
  locationId: string
  title: string
  sourceLabel: string
  description: string
  whyItMatters: string
  certainty: 'confirmed' | 'probable' | 'uncertain'
  unlocks?: string[]
}

export function StoryOpeningCard({ title, speaker, paragraphs, instruction, actionLabel = 'شروع بررسی روی نقشه', onContinue }: { title: string; speaker?: string; paragraphs: string[]; instruction: string; actionLabel?: string; onContinue: () => void }) {
  return <section className="hc-story-opening" data-testid="story-opening-card">
    <div className="hc-story-opening__mark" aria-hidden="true">نامه</div>
    <div className="hc-story-opening__heading"><span className="eyebrow">روایت جاری</span>{speaker && <small>{speaker}</small>}<h2>{title}</h2></div>
    <div className="hc-story-opening__copy">{paragraphs.slice(0, 3).map((paragraph) => <p key={paragraph}>{paragraph}</p>)}</div>
    <div className="hc-story-instruction"><span aria-hidden="true">۱</span><p>{instruction}</p></div>
    <button type="button" className="button button--gold" onClick={onContinue}>{actionLabel}</button>
  </section>
}

export function InvestigationPrompt({ investigatedCount, requiredCount }: { investigatedCount: number; requiredCount: number }) {
  return <section className="hc-investigation-scene" data-testid="investigation-prompt">
    <span className="eyebrow">بررسی مسیر بار</span>
    <h3>{investigatedCount ? 'یک سرنخ دیگر لازم است' : 'دو مکان را از روی نقشه انتخاب کنید'}</h3>
    <p>دفتر حاج صادق، دروازه بازار و باربری راه‌نو هرکدام بخش متفاوتی از مسیر بار را نشان می‌دهند.</p>
    <div className="hc-investigation-preview"><span aria-hidden="true">{investigatedCount}</span><p><strong>{investigatedCount} از {requiredCount} بررسی انجام شده.</strong> انتخاب سوم پس از کامل‌شدن دو بررسی بسته می‌شود.</p></div>
  </section>
}

export function LocationInvestigationCard({ title, identity, narrative, investigatedCount, requiredCount, onInvestigate }: { title: string; identity: string; narrative: string; investigatedCount: number; requiredCount: number; onInvestigate: () => void }) {
  return <section className="hc-investigation-scene" data-testid="location-investigation-scene">
    <div className="hc-investigation-scene__heading"><span className="hc-location-pin" aria-hidden="true" /><div><span className="eyebrow">مکان انتخاب‌شده</span><h3>{title}</h3><small>{identity}</small></div></div>
    <p>{narrative}</p>
    <div className="hc-investigation-preview"><span aria-hidden="true">{investigatedCount + 1}</span><p>این بررسی سه مدرک می‌دهد: یک واقعیت، یک تناقض و یک فرصت یا خطر عملی.</p></div>
    <button type="button" className="button button--gold" onClick={onInvestigate}>بررسی مدارک این مکان ({investigatedCount + 1} از {requiredCount})</button>
  </section>
}

const certaintyLabels = { confirmed: 'قطعی', probable: 'محتمل', uncertain: 'نامطمئن' } as const

export function EvidenceReveal({ locationName, evidence, investigatedCount, requiredCount, onContinue }: { locationName: string; evidence: EvidenceItem[]; investigatedCount: number; requiredCount: number; onContinue: () => void }) {
  const investigationComplete = investigatedCount >= requiredCount
  return <section className="hc-evidence-reveal" data-testid="evidence-reveal">
    <div><span className="eyebrow">مدارک {locationName}</span><h3>سه نکته که روی تصمیم شما اثر می‌گذارند</h3></div>
    <div className="hc-evidence-grid">{evidence.map((item) => <article className={`hc-evidence-card certainty-${item.certainty}`} key={item.id} data-testid={`evidence-${item.id}`}><header><strong>{item.title}</strong><span>{certaintyLabels[item.certainty]}</span></header><small>منبع: {item.sourceLabel}</small><p>{item.description}</p><dl><div><dt>اهمیت</dt><dd>{item.whyItMatters}</dd></div>{item.unlocks?.length ? <div><dt>ممکن است باز کند</dt><dd>{item.unlocks.join('، ')}</dd></div> : null}</dl></article>)}</div>
    <div className="hc-investigation-result"><strong>{investigatedCount} از {requiredCount} مکان بررسی شد.</strong><span>{investigationComplete ? 'اکنون فعالیت حجره و تصمیم نهایی باز است.' : 'برای روشن‌شدن مسیر بار، یک مکان دیگر را بررسی کنید.'}</span></div>
    <button type="button" className="button button--gold" onClick={onContinue}>{investigationComplete ? 'بردن نتیجه به حجره' : 'انتخاب مکان دوم'}</button>
  </section>
}

export function BusinessActionCard({ businessName, summary, implication, unlockedActions, canOfferPact, onOpenPact, onContinue }: { businessName: string; summary: string; implication: string; unlockedActions: string[]; canOfferPact: boolean; onOpenPact: () => void; onContinue: () => void }) {
  return <section className="hc-story-business" data-testid="story-business-action">
    <div className="hc-story-business__heading"><span className="hc-business-token" aria-hidden="true">حجره</span><div><span className="eyebrow">اثر روی کسب‌وکار شما</span><h3>{businessName}</h3></div></div>
    <p>{summary}</p>
    <div className="hc-business-impact"><span aria-hidden="true">مهر</span><p><strong>چرا مهم است؟</strong> {implication}</p></div>
    {unlockedActions.length > 0 && <div className="hc-unlocked-actions"><strong>راه‌های بازشده با تحقیق شما</strong><ul>{unlockedActions.map((action) => <li key={action}>{action}</li>)}</ul></div>}
    {canOfferPact && <div className="hc-context-letter"><span className="hc-context-letter__seal" aria-hidden="true">✦</span><div><strong>این خبر می‌تواند موضوع یک نامه باشد</strong><p>اگر لازم است تکه دفتر را با یک حجره مقایسه کنید، پیشنهاد همکاری بفرستید.</p></div><button type="button" className="hc-inline-action" onClick={onOpenPact}>نوشتن نامه یا پیمان</button></div>}
    <button type="button" className="button button--gold" onClick={onContinue}>آماده‌کردن تصمیم نهایی</button>
  </section>
}

export function DecisionSummary({ findings, businessMeaning }: { findings: string[]; businessMeaning: string }) {
  return <section className="hc-final-decision" data-testid="final-decision-summary">
    <div className="hc-final-decision__heading"><span className="hc-decision-document" aria-hidden="true">جمع‌بندی</span><div><span className="eyebrow">پیش از ثبت تصمیم</span><h3>آنچه حالا می‌دانید</h3></div></div>
    <dl><div><dt>کشف‌های شما</dt><dd><ul>{findings.map((finding) => <li key={finding}>{finding}</li>)}</ul></dd></div><div><dt>اثر روی حجره</dt><dd>{businessMeaning}</dd></div></dl>
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
