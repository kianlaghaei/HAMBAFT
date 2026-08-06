import type { TeamExperience } from '../../api/schemas'

export type ResolvedTeamScene = NonNullable<TeamExperience['teamScenePresentation']>
export type SceneAction = ResolvedTeamScene['authoredActions'][number]
export type SceneSheetContent = ResolvedTeamScene['storySheets'][number]
export type ActiveSceneSheet = { kind: 'opening' } | { kind: 'story'; id: string } | { kind: 'next' } | null

type Props = {
  scene: ResolvedTeamScene
  backgroundUrl?: string
  activeSheet: ActiveSceneSheet
  pendingActionId?: string
  onSelectHotspot: (storySheetId: string) => void
  onAction: (action: SceneAction, sheet?: SceneSheetContent) => void
  onCloseSheet: () => void
}

export function TeamSceneRenderer({ scene, backgroundUrl, activeSheet, pendingActionId, onSelectHotspot, onAction, onCloseSheet }: Props) {
  const selectedSheet = activeSheet?.kind === 'story' ? scene.storySheets.find((sheet) => sheet.id === activeSheet.id) : undefined
  return <div className="team-scene-renderer" data-testid="team-scene-renderer">
    {backgroundUrl ? <img className="team-scene-background" src={backgroundUrl} alt={scene.title} data-testid="team-scene-background" /> : <div className="team-scene-background team-scene-background--unavailable" aria-label="تصویر صحنه در دسترس نیست" />}
    <div className="team-market-markers" aria-label="مکان‌های صحنه">
      {scene.hotspots.map((hotspot) => <button
        type="button"
        key={hotspot.id}
        className={`team-market-marker${hotspot.investigated ? ' is-investigated' : ''}${selectedSheet?.id === hotspot.storySheetId ? ' is-selected' : ''}`}
        style={{ left: `${hotspot.x}%`, top: `${hotspot.y}%` }}
        aria-label={hotspot.label}
        onClick={() => onSelectHotspot(hotspot.storySheetId)}
      ><span className="team-market-marker__seal" aria-hidden="true" /><span className="team-market-marker__label">{hotspot.label}</span></button>)}
    </div>
    {activeSheet?.kind === 'opening' && <SceneSheet title={scene.title} subtitle={scene.subtitle} onClose={onCloseSheet}>
      <Narrative paragraphs={scene.openingNarrative} />
    </SceneSheet>}
    {selectedSheet && <StorySheet sheet={selectedSheet} investigated={scene.investigatedLocationIds.includes(selectedSheet.id)} pendingActionId={pendingActionId} onAction={onAction} onClose={onCloseSheet} />}
    {activeSheet?.kind === 'next' && scene.nextScenePresentation && <SceneSheet title={scene.nextScenePresentation.title} subtitle={scene.nextScenePresentation.subtitle} onClose={onCloseSheet}>
      <Narrative paragraphs={scene.nextScenePresentation.narrative} />
      <ActionList actions={scene.nextScenePresentation.actions} pendingActionId={pendingActionId} onAction={(action) => onAction(action)} />
    </SceneSheet>}
  </div>
}

function StorySheet({ sheet, investigated, pendingActionId, onAction, onClose }: { sheet: SceneSheetContent; investigated: boolean; pendingActionId?: string; onAction: Props['onAction']; onClose: () => void }) {
  const actions = sheet.actions.filter((action) => action.available && !(investigated && action.kind === 'RecordInvestigation'))
  return <SceneSheet title={sheet.title} subtitle={sheet.subtitle} onClose={onClose}>
    <Narrative paragraphs={sheet.narrative} />
    <div className="hc-evidence-grid">{sheet.evidence.map((evidence) => <article className={`hc-evidence-card certainty-${evidence.certainty}`} key={evidence.id}>
      <header><strong>{evidence.title}</strong><span>{certaintyLabel(evidence.certainty)}</span></header>
      <small>{evidence.sourceLabel}</small><p>{evidence.description}</p>
    </article>)}</div>
    <section className="hc-scene-sheet__why"><span className="eyebrow">چرا این مهم است؟</span><ul>{sheet.evidence.map((evidence) => <li key={evidence.id}>{evidence.whyItMatters}</li>)}</ul></section>
    {investigated && actions.length === 0 ? <button type="button" className="button button--gold hc-scene-sheet__action" onClick={onClose}>بازگشت به نقشه</button> : <ActionList actions={actions} pendingActionId={pendingActionId} onAction={(action) => onAction(action, sheet)} />}
  </SceneSheet>
}

function Narrative({ paragraphs }: { paragraphs: readonly string[] }) {
  return <div className="hc-scene-sheet__narrative">{paragraphs.map((paragraph, index) => <p key={`${index}:${paragraph}`}>{paragraph}</p>)}</div>
}

function ActionList({ actions, pendingActionId, onAction }: { actions: readonly SceneAction[]; pendingActionId?: string; onAction: (action: SceneAction) => void }) {
  const action = actions.find((candidate) => candidate.available)
  return action ? <button type="button" className="button button--gold hc-scene-sheet__action" disabled={pendingActionId === action.id} onClick={() => onAction(action)}>{action.label}</button> : null
}

function SceneSheet({ title, subtitle, onClose, children }: { title: string; subtitle?: string; onClose: () => void; children: React.ReactNode }) {
  return <div className="hc-scene-sheet-layer" data-testid="scene-sheet-layer">
    <button type="button" className="hc-scene-sheet-dim" aria-label="بازگشت به نقشه" onClick={onClose} />
    <aside className="hc-scene-sheet" aria-label={title} data-testid="scene-sheet">
      <header className="hc-scene-sheet__header"><div><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div><button type="button" className="hc-scene-sheet__close" onClick={onClose} aria-label="بستن و بازگشت به نقشه">×</button></header>
      <div className="hc-scene-sheet__body">{children}</div>
    </aside>
  </div>
}

function certaintyLabel(certainty: 'confirmed' | 'probable' | 'uncertain') {
  return { confirmed: 'قطعی', probable: 'محتمل', uncertain: 'نامطمئن' }[certainty]
}
