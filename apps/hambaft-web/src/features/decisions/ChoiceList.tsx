import type { TeamExperience } from '../../api/schemas'
import { rendererForStorylet } from './rendererRegistry'

type Storylet = TeamExperience['privateStorylets'][number]

export function ChoiceList({ storylet, disabled, onChoose }: { storylet: Storylet; disabled: boolean; onChoose: (choiceId: string) => void }) {
  return <div className="choices" data-renderer={rendererForStorylet(storylet.presentationTags)} aria-label="انتخاب‌های روایت">{storylet.choices.map((choice) => {
    const authored = choice.presentation
    return <article className={`contextual-choice${authored ? ` choice-${authored.renderer}` : ''}`} key={choice.id}>
      <div><span className="eyebrow">{authored?.renderer?.replaceAll('-', ' ') ?? 'تصمیم'}</span><h3>{authored?.shortTitle ?? choice.label}</h3>{authored?.action && <p>{authored.action}</p>}{authored?.immediateImplication && <p className="choice-implication">{authored.immediateImplication}</p>}{choice.shortOutcome && !authored && <p>{choice.shortOutcome}</p>}</div>
      {authored && <div className="choice-knowledge">{authored.knownCost && <p><strong>هزینه شناخته‌شده:</strong> {authored.knownCost}</p>}{authored.knownRisk && <p><strong>ریسک:</strong> {authored.knownRisk}</p>}{authored.unknownConsequence && <p className="unknown-consequence">بخشی از پیامد هنوز ناشناخته است.</p>}{authored.evidenceKind && <details><summary>بررسی سند مرتبط</summary><p>{evidenceLabel(authored.evidenceKind)}</p><small>دیدن این سند وضعیت بازی را تغییر نمی‌دهد.</small></details>}</div>}
      <button className="button" disabled={disabled} onClick={() => onChoose(choice.id)}>{authored?.shortTitle ?? choice.label}</button>
    </article>
  })}</div>
}

function evidenceLabel(kind: string) {
  return ({ 'ledger-fragment': 'تکه‌ای از دفتر که می‌توان پیش از تصمیم متن آن را بررسی کرد.', notice: 'پیش‌نمایش اطلاعیه و محل درج منبع.', 'route-record': 'ثبت مسیر، زمان خروج و نشان گاری.', 'guarantee-entry': 'مدخل ضمانت، مُهرها و طرف‌های آشکار آن.' } as Record<string, string>)[kind] ?? 'سند مرتبط با این تصمیم'
}
