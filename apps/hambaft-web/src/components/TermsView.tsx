import type { TermSchema } from '../api/schemas'
import { faDigits, termLabel } from '../design-system/presentation'
import { rendererForTerm } from '../features/decisions/rendererRegistry'

function renderValue(value: unknown): string {
  if (typeof value === 'boolean') return value ? 'بله' : 'خیر'
  if (typeof value === 'number') return faDigits(value)
  if (typeof value === 'string') return value
  return JSON.stringify(value)
}

export function TermsView({ terms }: { terms: Record<string, unknown> }) {
  return <dl className="terms">{Object.entries(terms).map(([key, value]) => <div key={key}><dt>{termLabel(key)}</dt><dd>{renderValue(value)}</dd></div>)}</dl>
}

export type TermDraft = Record<string, unknown>

export function TermsEditor({ schema, value, onChange }: { schema: TermSchema; value: TermDraft; onChange: (next: TermDraft) => void }) {
  const fields = ['Compound', 'CompoundDecision'].includes(schema.type) ? schema.fields : [schema]
  return <div className="form-grid" data-renderer={rendererForTerm(schema)}>{fields.map((field) => {
    const name = field.name ?? 'value'
    if (field.type === 'Boolean') return <label className="check-field" key={name}><input type="checkbox" checked={Boolean(value[name])} onChange={(event) => onChange({ ...value, [name]: event.target.checked })} /><span>{termLabel(name)}</span></label>
    if (['SingleChoice', 'TargetSelection', 'DocumentSelection', 'EvidenceSelection'].includes(field.type)) return <label className="field" key={name}><span>{termLabel(name)}</span><select value={String(value[name] ?? '')} required={field.required} onChange={(event) => onChange({ ...value, [name]: event.target.value })}><option value="">—</option>{field.fields.map((option) => <option key={option.name} value={option.name ?? ''}>{termLabel(option.name ?? '')}</option>)}</select></label>
    if (field.type === 'MultipleChoice') {
      const selected = Array.isArray(value[name]) ? value[name] as string[] : []
      return <fieldset className="decision-field" key={name}><legend>{termLabel(name)}</legend>{field.fields.map((option) => <label className="check-field" key={option.name}><input type="checkbox" checked={selected.includes(option.name ?? '')} onChange={(event) => onChange({ ...value, [name]: event.target.checked ? [...selected, option.name ?? ''] : selected.filter((item) => item !== option.name) })} />{termLabel(option.name ?? '')}</label>)}</fieldset>
    }
    if (field.type === 'RankedChoice') {
      const selected = Array.isArray(value[name]) ? value[name] as string[] : field.fields.map((option) => option.name ?? '')
      const move = (index: number, direction: -1 | 1) => { const next = [...selected]; const other = index + direction; [next[index], next[other]] = [next[other]!, next[index]!]; onChange({ ...value, [name]: next }) }
      return <fieldset className="decision-field" key={name}><legend>{termLabel(name)}</legend>{selected.map((option, index) => <div className="ranked-option" key={option}><span>{faDigits(index + 1)}. {termLabel(option)}</span><button type="button" disabled={index === 0} onClick={() => move(index, -1)}>↑</button><button type="button" disabled={index === selected.length - 1} onClick={() => move(index, 1)}>↓</button></div>)}</fieldset>
    }
    if (field.type === 'NumericAllocation') {
      const allocation = value[name] && typeof value[name] === 'object' ? value[name] as Record<string, number> : {}
      return <fieldset className="decision-field" key={name}><legend>{termLabel(name)}</legend>{field.fields.map((option) => <label className="field" key={option.name}><span>{termLabel(option.name ?? '')}</span><input type="number" min={option.minimum ?? 0} max={option.maximum ?? field.maximum ?? undefined} value={allocation[option.name ?? ''] ?? 0} onChange={(event) => onChange({ ...value, [name]: { ...allocation, [option.name ?? '']: Number(event.target.value) } })} /></label>)}</fieldset>
    }
    if (field.type === 'NumericRange') return <label className="field range-field" key={name}><span>{termLabel(name)}: {faDigits(String(value[name] ?? field.minimum ?? 0))}</span><input type="range" min={field.minimum ?? 0} max={field.maximum ?? 100} value={Number(value[name] ?? field.minimum ?? 0)} onChange={(event) => onChange({ ...value, [name]: Number(event.target.value) })} /></label>
    if (field.type === 'ShortDeclaration') return <label className="field" key={name}><span>{termLabel(name)}</span><textarea maxLength={field.maximumLength ?? undefined} required={field.required} value={String(value[name] ?? '')} onChange={(event) => onChange({ ...value, [name]: event.target.value })} /></label>
    return <label className="field" key={name}><span>{termLabel(name)}{field.required ? ' *' : ''}</span><input name={name} type={field.type === 'Numeric' ? 'number' : 'text'} min={field.minimum ?? undefined} max={field.maximum ?? undefined} maxLength={field.maximumLength ?? undefined} required={field.required} value={String(value[name] ?? '')} onChange={(event) => onChange({ ...value, [name]: field.type === 'Numeric' ? Number(event.target.value) : event.target.value })} /></label>
  })}</div>
}
