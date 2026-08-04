import type { TeamExperience } from '../../api/schemas'
import { rendererForStorylet } from './rendererRegistry'

type Storylet = TeamExperience['privateStorylets'][number]

export function ChoiceList({ storylet, disabled, onChoose }: { storylet: Storylet; disabled: boolean; onChoose: (choiceId: string) => void }) {
  return <div className="choices" data-renderer={rendererForStorylet(storylet.presentationTags)} aria-label="انتخاب‌های روایت">{storylet.choices.map((choice) => <button key={choice.id} disabled={disabled} onClick={() => onChoose(choice.id)}><strong>{choice.label}</strong>{choice.shortOutcome && <span>{choice.shortOutcome}</span>}</button>)}</div>
}
