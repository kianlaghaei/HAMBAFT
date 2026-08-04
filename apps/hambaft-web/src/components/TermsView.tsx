import type { TermSchema } from '../api/schemas'
import { faDigits, termLabel } from '../design-system/presentation'

function renderValue(value: unknown): string {
  if (typeof value === 'boolean') return value ? 'بله' : 'خیر'
  if (typeof value === 'number') return faDigits(value)
  if (typeof value === 'string') return value
  return JSON.stringify(value)
}

export function TermsView({ terms }: { terms: Record<string, unknown> }) {
  return <dl className="terms">{Object.entries(terms).map(([key, value]) => <div key={key}><dt>{termLabel(key)}</dt><dd>{renderValue(value)}</dd></div>)}</dl>
}

export type TermDraft = Record<string, string | number | boolean>

export function TermsEditor({ schema, value, onChange }: { schema: TermSchema; value: TermDraft; onChange: (next: TermDraft) => void }) {
  const fields = schema.type === 'Compound' ? schema.fields : [schema]
  return <div className="form-grid">{fields.map((field) => {
    const name = field.name ?? 'value'
    if (field.type === 'Boolean') return <label className="check-field" key={name}><input type="checkbox" checked={Boolean(value[name])} onChange={(event) => onChange({ ...value, [name]: event.target.checked })} /><span>{termLabel(name)}</span></label>
    return <label className="field" key={name}><span>{termLabel(name)}{field.required ? ' *' : ''}</span><input name={name} type={field.type === 'Numeric' ? 'number' : 'text'} min={field.minimum ?? undefined} max={field.maximum ?? undefined} maxLength={field.maximumLength ?? undefined} required={field.required} value={String(value[name] ?? '')} onChange={(event) => onChange({ ...value, [name]: field.type === 'Numeric' ? Number(event.target.value) : event.target.value })} /></label>
  })}</div>
}
