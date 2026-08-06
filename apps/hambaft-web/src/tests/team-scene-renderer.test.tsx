import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { TeamSceneRenderer, type ResolvedTeamScene } from '../features/team-scene/TeamSceneRenderer'

const scene = {
  sceneId: 'private-scene-id', title: 'بازار تیم', subtitle: 'روایت مجاز', backgroundAssetId: 'asset.svg', backgroundAssetUrl: '/api/story/scene-asset',
  openingNarrative: ['بند آغازین'], objective: '', requiredInvestigationCount: 1, investigationLocations: [], businessActivity: null, contextualPacts: [], finalDecisionPresentation: null, marketReactions: [], investigatedLocationIds: [], revealedEvidenceIds: [],
  hotspots: [{ id: 'private-marker-id', label: 'ساعت بازار', x: 42, y: 33, storySheetId: 'private-sheet-id', shared: true, investigated: false, targetDescription: 'ساعت', expectedVisibleObject: 'ساعت بازار', required: true, displayOrder: 1 }],
  storySheets: [{ id: 'private-sheet-id', title: 'ساعت بازار', subtitle: 'بالای گذر', narrative: ['بند یک', 'بند دو', 'بند سه'], evidence: [{ id: 'private-evidence-id', title: 'مهر قاب', sourceLabel: 'قاب ساعت', description: 'مهر شکسته است.', whyItMatters: 'زمان روایت را روشن می‌کند.', certainty: 'confirmed', unlocks: [] }], actions: [{ id: 'private-action-id', label: 'ثبت بررسی', kind: 'RecordInvestigation', targetId: 'private-sheet-id', available: true }] }],
  authoredActions: [{ id: 'map-action', label: 'انتخاب مکان', kind: 'ShowMap', targetId: null, available: true }], nextScenePresentation: null,
} satisfies ResolvedTeamScene

describe('TeamSceneRenderer', () => {
  it('renders only the resolved image and authored hotspot content', () => {
    const select = vi.fn()
    render(<TeamSceneRenderer scene={scene} backgroundUrl="blob:authorized-scene" activeSheet={null} onSelectHotspot={select} onAction={vi.fn()} onCloseSheet={vi.fn()} />)
    expect(screen.getByRole('img', { name: 'بازار تیم' })).toHaveAttribute('src', 'blob:authorized-scene')
    fireEvent.click(screen.getByRole('button', { name: 'ساعت بازار' }))
    expect(select).toHaveBeenCalledWith('private-sheet-id')
    expect(screen.queryByText('private-marker-id')).not.toBeInTheDocument()
  })

  it('opens the large in-map sheet and dispatches its authored action', () => {
    const action = vi.fn()
    render(<TeamSceneRenderer scene={scene} backgroundUrl="blob:authorized-scene" activeSheet={{ kind: 'story', id: 'private-sheet-id' }} onSelectHotspot={vi.fn()} onAction={action} onCloseSheet={vi.fn()} />)
    expect(screen.getByTestId('scene-sheet')).toHaveTextContent('بند یک')
    expect(screen.getByTestId('scene-sheet')).toHaveTextContent('زمان روایت را روشن می‌کند.')
    fireEvent.click(screen.getByRole('button', { name: 'ثبت بررسی' }))
    expect(action).toHaveBeenCalledWith(scene.storySheets[0]!.actions[0]!, scene.storySheets[0])
    expect(screen.queryByText('private-sheet-id')).not.toBeInTheDocument()
  })
})
