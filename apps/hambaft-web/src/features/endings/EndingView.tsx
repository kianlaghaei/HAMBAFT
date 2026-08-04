import type { z } from 'zod'
import { endingSchema } from '../../api/schemas'
import { endingEvidenceText } from '../../design-system/presentation'

type Ending = z.infer<typeof endingSchema>

export function EndingView({ ending, world = false }: { ending: Ending; world?: boolean }) {
  return <article className={`ending-card ${world ? 'ending-card--world' : ''}`}><span className="ending-ornament" aria-hidden="true">✦</span><p className="eyebrow">{world ? 'سرانجام بازار' : 'سرانجام حجره'}</p><h2>{ending.title}</h2><div className="narrative-copy">{ending.paragraphs.map((paragraph, index) => <p key={index}>{paragraph}</p>)}</div>{ending.evidence.length > 0 && <section className="evidence"><h3>ردّ تصمیم‌ها در این سرانجام</h3><ul>{ending.evidence.map((item, index) => <li key={`${item.referenceId ?? item.stableId}-${index}`}>{endingEvidenceText(item)}</li>)}</ul></section>}</article>
}
