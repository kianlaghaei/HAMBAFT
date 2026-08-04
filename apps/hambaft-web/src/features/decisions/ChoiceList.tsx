import type { TeamExperience } from '../../api/schemas'

type Storylet = TeamExperience['privateStorylets'][number]

export function ChoiceList({ storylet, disabled, onChoose }: { storylet: Storylet; disabled: boolean; onChoose: (choiceId: string) => void }) {
  return <div className="choices" aria-label="انتخاب‌های روایت">{storylet.choices.map((choice) => <button key={choice.id} disabled={disabled} onClick={() => onChoose(choice.id)}><strong>{choice.label}</strong>{choice.shortOutcome && <span>{choice.shortOutcome}</span>}</button>)}</div>
}
