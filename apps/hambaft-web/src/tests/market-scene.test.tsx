import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { MarketScene } from '../features/visual-world/MarketScene'
import { marketLayers } from '../features/visual-world/types'
import { useUiStore } from '../state/uiStore'
import { publicWorldFixture } from './fixtures'

const destroy = vi.fn()
const layerLabels: string[] = []
class MockContainer {
  label?: string; x = 0; y = 0
  position = { set: vi.fn() }; scale = { set: vi.fn() }
  constructor(options?: { label?: string; x?: number; y?: number }) { Object.assign(this, options); if (options?.label) layerLabels.push(options.label) }
  addChild = vi.fn()
}
class MockGraphics {
  position = { set: vi.fn() }; alpha = 1; rotation = 0
  rect() { return this } roundRect() { return this } ellipse() { return this } circle() { return this } poly() { return this }
  fill() { return this } stroke() { return this } cut() { return this } moveTo() { return this } lineTo() { return this }
  quadraticCurveTo() { return this } bezierCurveTo() { return this }
}
class MockText { anchor = { set: vi.fn() }; y = 0; constructor(options: unknown) { void options } }
class MockApplication {
  canvas = document.createElement('canvas'); stage = new MockContainer(); screen = { width: 1000, height: 700 }
  renderer = { on: vi.fn(), off: vi.fn() }; ticker = { add: vi.fn() }
  init = vi.fn().mockResolvedValue(undefined); destroy = destroy
}
vi.mock('pixi.js', () => ({ Application: MockApplication, Container: MockContainer, Graphics: MockGraphics, Text: MockText }))

describe('market renderer modes', () => {
  it('renders the accessible CSS/SVG fallback and keeps gameplay visible', () => {
    useUiStore.setState({ visualMode: 'fallback', reducedMotion: false })
    render(<MarketScene world={publicWorldFixture} />)
    expect(screen.getByRole('img', { name: /نقشه زنده بازار/ })).toBeInTheDocument()
    expect(screen.getByText('دفتر بسته حاج صادق')).toBeInTheDocument()
  })

  it('honors reduced motion even when high mode was selected', () => {
    useUiStore.setState({ visualMode: 'high', reducedMotion: true })
    const { container } = render(<MarketScene world={publicWorldFixture} />)
    expect(container.querySelector('.visual-reduced .is-reduced')).toBeInTheDocument()
    expect(container.querySelector('canvas')).not.toBeInTheDocument()
  })

  it('mounts Pixi with all stable layers and destroys it during React cleanup', async () => {
    destroy.mockClear(); layerLabels.length = 0
    useUiStore.setState({ visualMode: 'high', reducedMotion: false })
    const view = render(<MarketScene world={publicWorldFixture} />)
    await waitFor(() => expect(screen.getByTestId('pixi-market').querySelector('canvas')).toBeInTheDocument())
    expect(layerLabels).toEqual(expect.arrayContaining([...marketLayers]))
    view.unmount()
    expect(destroy).toHaveBeenCalled()
  })
})
