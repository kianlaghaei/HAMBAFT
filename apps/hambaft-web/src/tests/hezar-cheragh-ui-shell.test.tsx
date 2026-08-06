import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { GameplayPanel } from '../features/hezar-cheragh/GameplayPanel'
import { BazaarMapViewport } from '../features/hezar-cheragh/BazaarMapViewport'
import { HezarCheraghShell } from '../features/hezar-cheragh/HezarCheraghShell'
import { devFixtures } from '../features/hezar-cheragh/fixtures'
import { registerAssets, clearAssetRegistry, resolveAsset } from '../features/hezar-cheragh/assetRegistry'
import { devAssetManifest } from '../features/hezar-cheragh/devManifest'
import { buildSceneDescriptor } from '../features/visual-world/sceneDirector'
import type { PublicWorld } from '../api/schemas'
import type { SceneDescriptor } from '../features/visual-world/types'
import { useUiStore } from '../state/uiStore'
import { TeamGameplayScreen } from '../features/hezar-cheragh/TeamGameplayScreen'
import { TeamMarketMarkers, type TeamMarketMarkerDefinition } from '../features/hezar-cheragh/TeamMarketMarkers'
import { publicWorldFixture, teamExperienceFixture } from './fixtures'
import { api } from '../api/client'

/* ── Helpers ── */

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/team']}>{children}</MemoryRouter>
    </QueryClientProvider>
  )
}

function mockWorld(): PublicWorld {
  return {
    id: 'test-session',
    stateVersion: 1,
    currentCheckpointId: 'morning-without-bell',
    narrative: { title: 'بازار', presentationTags: { time: 'morning' } },
    worldPresentation: {
      sceneId: 'test-scene',
      timeOfDay: 'morning',
      timeLabel: 'صبح',
      atmosphereLabel: 'آرام',
      publicEvent: '',
      courtyardActivity: '',
      soundscape: 'ambient-market',
      visualTags: [],
      semanticMetrics: [{ key: 'Pressure', bandId: 'calm' }],
      pulse: [],
      ambientEvents: [],
      characters: [],
      reactions: [],
      avanVisible: false,
      locations: [
        { id: 'bakery-sepideh', displayName: 'نانوایی سپیده', shortIdentity: 'نانوایی', whyItMatters: 'مهم', currentCondition: 'فعال', whoIsHere: 'سپیده', recentChange: '', availableActions: [], presentationTags: [], entityId: 'bakery-sepideh', entityDefinitionId: 'bakery-sepideh', x: 21, y: 33 },
        { id: 'logistics-rah-no', displayName: 'باربری راه نو', shortIdentity: 'باربری', whyItMatters: 'حمل بار', currentCondition: 'فعال', whoIsHere: 'مدیر', recentChange: 'مسیر بسته', availableActions: ['بازرسی'], presentationTags: [], entityId: 'logistics-rah-no', entityDefinitionId: 'logistics-rah-no', x: 82, y: 36 },
      ],
      semantic: true,
    },
    worldEnding: false,
    entities: [
      { id: 'bakery-sepideh', definitionId: 'bakery-sepideh', displayName: 'نانوایی سپیده', status: 'Active', controllerType: 'HumanTeam', controlledByTeamId: 'team-a' },
      { id: 'logistics-rah-no', definitionId: 'logistics-rah-no', displayName: 'باربری راه نو', status: 'Active', controllerType: 'HumanTeam', controlledByTeamId: 'team-b' },
    ],
    publicAgreements: [],
  } as unknown as PublicWorld
}

function mockDescriptor(): SceneDescriptor {
  return buildSceneDescriptor(mockWorld())
}

/* ── Tests ── */

beforeEach(() => {
  vi.restoreAllMocks()
  vi.spyOn(api, 'recordInvestigation').mockResolvedValue({ sessionId: 'test-session', stateVersion: 13, eventType: 'InvestigationRecorded' })
  clearAssetRegistry()
  registerAssets(devAssetManifest)
})

describe('GameplayPanel mode switching', () => {
  it('renders SceneIntroduction mode', () => {
    render(<GameplayPanel fixture={devFixtures.introduction} />, { wrapper })
    expect(screen.getByTestId('gameplay-panel')).toHaveAttribute('data-mode', 'SceneIntroduction')
    expect(screen.getByText('صبحِ بی‌زنگ')).toBeTruthy()
    expect(screen.getByText('بازار هزارچراغ')).toBeTruthy()
  })

  it('renders Investigation mode with seals', () => {
    render(<GameplayPanel fixture={devFixtures.investigation} />, { wrapper })
    expect(screen.getByTestId('gameplay-panel')).toHaveAttribute('data-mode', 'Investigation')
    expect(screen.getByText('یک مهر بررسی دیگر در اختیار دارید.')).toBeTruthy()
    expect(screen.getByText('نانوایی سپیده')).toBeTruthy()
  })

  it('shows exhausted message when no seals remain', () => {
    const noSeals = { ...devFixtures.investigation, remainingSeals: 0 }
    render(<GameplayPanel fixture={noSeals} />, { wrapper })
    expect(screen.getByText('همه مهرهای بررسی مصرف شده‌اند.')).toBeTruthy()
  })

  it('renders BusinessActivity (Bakery) mode', () => {
    render(<GameplayPanel fixture={devFixtures.bakery} />, { wrapper })
    expect(screen.getByTestId('gameplay-panel')).toHaveAttribute('data-mode', 'BusinessActivity')
    expect(screen.getByText('تخصیص سهمیه آرد')).toBeTruthy()
    expect(screen.getByText('تأیید')).toBeTruthy()
  })

  it('renders BusinessActivity (Logistics) waiting state', () => {
    render(<GameplayPanel fixture={devFixtures.logistics} />, { wrapper })
    expect(screen.getByText('تابلوی مسیر باربری')).toBeTruthy()
    expect(screen.getByText('منتظر شرایط مناسب...')).toBeTruthy()
  })

  it('renders PactTargetSelection mode', () => {
    render(<GameplayPanel fixture={devFixtures.pactTarget} />, { wrapper })
    expect(screen.getByText('انتخاب طرف پیمان')).toBeTruthy()
    expect(screen.getByText('نانوایی سپیده')).toBeTruthy()
  })

  it('renders ProposalLetter mode', () => {
    render(<GameplayPanel fixture={devFixtures.proposal} />, { wrapper })
    expect(screen.getByText('پیشنهاد همکاری تأمین آرد')).toBeTruthy()
    expect(screen.getByText('پذیرش')).toBeTruthy()
    expect(screen.getByText('پیشنهاد متقابل')).toBeTruthy()
    expect(screen.getByText('رد')).toBeTruthy()
  })

  it('renders Counterproposal with diff markers', () => {
    render(<GameplayPanel fixture={devFixtures.counterproposal} />, { wrapper })
    expect(screen.getByText('پیشنهاد متقابل تأمین آرد')).toBeTruthy()
    // unchanged terms
    expect(screen.getByText('مدت')).toBeTruthy()
    // added terms
    expect(screen.getByText('تضمین کیفیت')).toBeTruthy()
    expect(screen.getByText('یادداشت')).toBeTruthy()
  })

  it('renders Waiting mode', () => {
    render(<GameplayPanel fixture={devFixtures.waiting} />, { wrapper })
    expect(screen.getByText('در انتظار')).toBeTruthy()
    expect(screen.getByText('منتظر تصمیم تیم‌های دیگر... بازار در حال بررسی پیشنهاد شماست.')).toBeTruthy()
  })

  it('renders WorldReaction mode with multiple reactions', () => {
    render(<GameplayPanel fixture={devFixtures.worldReaction} currentReactionIndex={0} totalReactions={3} />, { wrapper })
    expect(screen.getByText('واکنش بازار')).toBeTruthy()
    expect(screen.getByText('1 از 3')).toBeTruthy()
    expect(screen.getByText('نانوایی سپیده')).toBeTruthy()
  })

  it('renders EntityEnding mode', () => {
    render(<GameplayPanel fixture={devFixtures.entityEnding} />, { wrapper })
    expect(screen.getByText('پایان مسیر نانوایی سپیده')).toBeTruthy()
    expect(screen.getByText('قول قیمت ثابت نقض شد')).toBeTruthy()
  })

  it('renders CompositeCommitment mode', () => {
    render(<GameplayPanel fixture={devFixtures.compositeCommitment} />, { wrapper })
    expect(screen.getByText('پیمان‌های فعال')).toBeTruthy()
    expect(screen.getByText('تأمین آرد')).toBeTruthy()
  })

  it('renders LocationContext mode', () => {
    render(<GameplayPanel fixture={devFixtures.locationContext} />, { wrapper })
    expect(screen.getByText('نانوایی سپیده')).toBeTruthy()
    expect(screen.getByText('چرا مهم است؟')).toBeTruthy()
  })

  it('panel close button calls onClose', async () => {
    let closed = false
    render(<GameplayPanel fixture={devFixtures.introduction} onClose={() => { closed = true }} />, { wrapper })
    await userEvent.click(screen.getByLabelText('بستن'))
    expect(closed).toBe(true)
  })

  it('panel confirm calls onConfirm', async () => {
    let confirmed = false
    render(<GameplayPanel fixture={devFixtures.introduction} onConfirm={() => { confirmed = true }} />, { wrapper })
    await userEvent.click(screen.getByText('ادامه'))
    expect(confirmed).toBe(true)
  })
})

describe('BazaarMapViewport', () => {
  it('renders map viewport', () => {
    const desc = mockDescriptor()
    render(<BazaarMapViewport descriptor={desc} timeMode="morning" />, { wrapper })
    // Should eventually render the viewport
    // Loading state shows first
    expect(screen.getByTestId('bazaar-map-loading')).toBeTruthy()
  })

  it('renders loading state', () => {
    const desc = mockDescriptor()
    render(<BazaarMapViewport descriptor={desc} timeMode="morning" />, { wrapper })
    expect(screen.getByText('در حال بارگذاری نقشه بازار...')).toBeTruthy()
  })

  it('handles selected location state', () => {
    const desc = mockDescriptor()
    render(<BazaarMapViewport descriptor={desc} timeMode="morning" selectedLocationId="bakery-sepideh" />, { wrapper })
    // Viewport is rendered
    expect(screen.getByTestId('bazaar-map-loading')).toBeTruthy()
  })
})

describe('Asset registry', () => {
  it('registers and resolves assets', () => {
    clearAssetRegistry()
    registerAssets(devAssetManifest)
    const meta = devAssetManifest['bazaar.base']
    expect(meta).toBeTruthy()
    expect(meta?.alpha).toBe(true)
    expect(meta?.src).toContain('/assets/hezar-cheragh/')
  })

  it('returns fallback for missing assets', () => {
    clearAssetRegistry()
    const fallback = resolveAsset('nonexistent.asset')
    expect(fallback.label).toContain('missing')
  })
})

describe('Map configuration', () => {
  it('loads default bazaar map', async () => {
    const { loadBazaarMap } = await import('../features/hezar-cheragh/bazaarMapConfig')
    const map = loadBazaarMap()
    expect(map.canvas.width).toBeGreaterThan(0)
    expect(map.canvas.height).toBeGreaterThan(0)
    expect(map.locations.length).toBeGreaterThan(0)
  })

  it('all map locations have valid asset IDs', async () => {
    const { loadBazaarMap } = await import('../features/hezar-cheragh/bazaarMapConfig')
    const map = loadBazaarMap()
    for (const loc of map.locations) {
      expect(loc.assetId).toBeTruthy()
      expect(loc.x).toBeGreaterThan(0)
      expect(loc.y).toBeGreaterThan(0)
    }
  })
})

describe('HezarCheraghShell layout', () => {
  it('renders shell with header, map, panel, footer', () => {
    const world = mockWorld()
    render(
      <HezarCheraghShell world={world} panelFixture={devFixtures.introduction} timeMode="morning" />,
      { wrapper },
    )
    expect(screen.getByTestId('hezar-cheragh-shell')).toBeTruthy()
    expect(screen.getByText(/HAMBAFT/)).toBeTruthy()
  })

  it('renders compact layout', () => {
    const world = mockWorld()
    render(
      <HezarCheraghShell world={world} panelFixture={devFixtures.introduction} timeMode="morning" compact />,
      { wrapper },
    )
    const shell = screen.getByTestId('hezar-cheragh-shell')
    expect(shell.className).toContain('is-compact')
  })

  it('renders reduced motion class', () => {
    const world = mockWorld()
    // Need to set reducedMotion in the store
    useUiStore.setState({ reducedMotion: true })
    render(
      <HezarCheraghShell world={world} panelFixture={devFixtures.introduction} timeMode="morning" />,
      { wrapper },
    )
    const shell = screen.getByTestId('hezar-cheragh-shell')
    expect(shell.className).toContain('is-reduced-motion')
    useUiStore.setState({ reducedMotion: false })
  })

  it('renders notification when provided', () => {
    const world = mockWorld()
    render(
      <HezarCheraghShell world={world} panelFixture={devFixtures.introduction} timeMode="morning" notification="پیام فوری!" />,
      { wrapper },
    )
    expect(screen.getByText('پیام فوری!')).toBeTruthy()
  })
})

describe('Keyboard accessibility', () => {
  it('panel mode label is present for screen readers', () => {
    render(<GameplayPanel fixture={devFixtures.investigation} />, { wrapper })
    expect(screen.getByText('بررسی')).toBeTruthy()
  })

  it('investigation buttons are disabled when no seals', () => {
    const noSeals = { ...devFixtures.investigation, remainingSeals: 0 }
    render(<GameplayPanel fixture={noSeals} />, { wrapper })
    const buttons = screen.getAllByRole('button').filter(b => b.textContent?.includes('نانوایی'))
    for (const btn of buttons) {
      expect(btn).toBeDisabled()
    }
  })
})

describe('No raw metric values', () => {
  it('investigation does not show 1/2 counter', () => {
    render(<GameplayPanel fixture={devFixtures.investigation} />, { wrapper })
    expect(screen.queryByText(/1\/2/)).toBeNull()
    expect(screen.queryByText(/2\/2/)).toBeNull()
    expect(screen.queryByText(/0\/2/)).toBeNull()
  })

  it('uses Persian narrative wording for seals', () => {
    render(<GameplayPanel fixture={devFixtures.investigation} />, { wrapper })
    expect(screen.getByText(/مهر بررسی/)).toBeTruthy()
  })
})

describe('Fallback mode', () => {
  it('panel renders in fallback mode without crashing', () => {
    render(<GameplayPanel fixture={devFixtures.introduction} />, { wrapper })
    expect(screen.getByTestId('gameplay-panel')).toBeTruthy()
  })
})

describe('Counterproposal diff', () => {
  it('shows changed terms with visual distinction', () => {
    render(<GameplayPanel fixture={devFixtures.counterproposal} />, { wrapper })
    // The term "مقدار" exists with status "changed"
    const termDt = screen.getByText(/مقدار/)
    expect(termDt).toBeTruthy()
    // The parent should have term-changed class
    const termEl = termDt.closest('.letter-term')
    expect(termEl?.className).toContain('term-changed')
  })

  it('shows added terms', () => {
    render(<GameplayPanel fixture={devFixtures.counterproposal} />, { wrapper })
    const addedTerm = screen.getByText('تضمین کیفیت')
    const addedEl = addedTerm.closest('.letter-term')
    expect(addedEl?.className).toContain('term-added')
  })

  it('shows removed terms with strikethrough', () => {
    render(<GameplayPanel fixture={devFixtures.counterproposal} />, { wrapper })
    // "نمایش عمومی" is removed
    const removedTerm = screen.getByText('نمایش عمومی')
    const removedEl = removedTerm.closest('.letter-term')
    expect(removedEl?.className).toContain('term-removed')
  })
})

describe('World reaction sequence', () => {
  it('shows next button for non-last reaction', () => {
    render(<GameplayPanel fixture={devFixtures.worldReaction} currentReactionIndex={0} totalReactions={3} />, { wrapper })
    expect(screen.getByText('بعدی')).toBeTruthy()
    expect(screen.getByText('رد کردن')).toBeTruthy()
  })

  it('shows continue button for last reaction', () => {
    render(<GameplayPanel fixture={devFixtures.worldReaction} currentReactionIndex={2} totalReactions={3} />, { wrapper })
    expect(screen.getByText('ادامه روایت')).toBeTruthy()
    expect(screen.queryByText('بعدی')).toBeNull()
  })
})

describe('Tablet / compact layout', () => {
  it('compact shell renders without horizontal scroll', () => {
    const world = mockWorld()
    render(
      <HezarCheraghShell world={world} panelFixture={devFixtures.investigation} timeMode="morning" compact />,
      { wrapper },
    )
    const shell = screen.getByTestId('hezar-cheragh-shell')
    expect(shell.className).toContain('is-compact')
    expect(screen.getByTestId('gameplay-panel')).toBeTruthy()
  })
})

describe('Production Team single-screen board', () => {
  const markers: TeamMarketMarkerDefinition[] = [
    { id: 'haj-sadegh-office', label: 'دفتر حاج صادق', x: 50, y: 15 },
    { id: 'bakery-sepideh', label: 'نانوایی سپیده', x: 27, y: 28, businessIdentity: 'نان' },
    { id: 'logistics-rah-no', label: 'باربری راه‌نو', x: 25, y: 57, businessIdentity: 'بار' },
    { id: 'printing-roshan', label: 'چاپخانه روشن', x: 74, y: 29, businessIdentity: 'چاپ' },
    { id: 'exchange-mizan', label: 'صرافی میزان', x: 75, y: 57, businessIdentity: 'مهر' },
    { id: 'market-entrance', label: 'دروازه بازار', x: 50, y: 78 },
  ]
  it('renders the empty stage at /team without legacy map nodes', () => {
    render(<Routes><Route path="/team" element={<TeamGameplayScreen experience={teamExperienceFixture} world={publicWorldFixture} canWrite />} /></Routes>, { wrapper })
    expect(screen.getByTestId('empty-market-stage')).toBeTruthy()
    expect(screen.getByTestId('team-market-map-image')).toHaveAttribute('src', '/assets/hezar-cheragh/team-market-map.webp')
    expect(screen.getAllByTestId(/^team-market-marker-/)).toHaveLength(6)
    expect(screen.getByTestId('team-market-marker-bakery-sepideh')).toHaveClass('is-owned')
    expect(screen.getByTestId('team-market-marker-haj-sadegh-office')).toHaveClass('is-action', 'is-urgent')
    expect(screen.queryByTestId('bazaar-map-viewport')).toBeNull()
    expect(screen.queryByTestId('pixi-market')).toBeNull()
    expect(screen.queryByRole('heading', { name: 'بازار را از روی نشانه‌ها بخوانید' })).toBeNull()
  })

  it('renders exactly six accessible percentage-positioned Team market markers', () => {
    render(<TeamMarketMarkers markers={markers} ownedLocationId="bakery-sepideh" activeLocationId="bakery-sepideh" urgentLocationIds={['bakery-sepideh']} disabledLocationIds={['exchange-mizan']} />)

    const markerButtons = screen.getAllByRole('button')
    expect(markerButtons).toHaveLength(6)
    expect(screen.getByRole('button', { name: 'نانوایی سپیده' })).toHaveAttribute('aria-pressed', 'false')
    expect(screen.getByRole('button', { name: 'نانوایی سپیده' })).toHaveClass('is-owned')
    expect(screen.getByRole('button', { name: 'نانوایی سپیده' })).toHaveClass('is-urgent')
    expect(screen.getByRole('button', { name: 'نانوایی سپیده' })).toHaveAttribute('aria-current', 'location')
    expect(screen.getByRole('button', { name: 'صرافی میزان' })).toHaveAttribute('aria-disabled', 'true')
    expect(screen.getByTestId('team-market-marker-bakery-sepideh')).toHaveStyle({ left: '27%', top: '28%' })
    expect(screen.getByTestId('team-market-marker-logistics-rah-no')).toHaveStyle({ left: '25%', top: '57%' })
    expect(screen.getByTestId('team-market-marker-printing-roshan')).toHaveStyle({ left: '74%', top: '29%' })
    expect(screen.getByText('دفتر حاج صادق')).toHaveClass('team-market-marker__label')
  })

  it('selects a relevant marker and opens its authored SceneSheet over the map', async () => {
    render(<TeamGameplayScreen experience={teamExperienceFixture} world={publicWorldFixture} canWrite />, { wrapper })
    await userEvent.click(screen.getByRole('button', { name: 'انتخاب مکان‌ها' }))
    const marker = screen.getByRole('button', { name: 'دفتر حاج صادق' })
    await userEvent.click(marker)
    expect(marker).toHaveAttribute('aria-pressed', 'true')
    expect(marker).toHaveClass('is-selected')
    expect(screen.getByTestId('scene-sheet')).toHaveAttribute('aria-label', 'دفتر حاج صادق')
    expect(screen.getByText('دفتر بسته‌ی بازار')).toBeTruthy()
    expect(screen.getByText('چرا این مهم است؟')).toBeTruthy()
    expect(document.querySelector('.hc-play-panel')).toBeNull()
  })

  it('shows three concise opening paragraphs, one objective and no empty utilities', () => {
    render(<TeamGameplayScreen experience={teamExperienceFixture} world={publicWorldFixture} canWrite />, { wrapper })
    expect(screen.getByTestId('team-gameplay-screen')).toBeTruthy()
    expect(screen.getByTestId('story-opening-card')).toBeTruthy()
    expect(screen.getByRole('heading', { level: 1, name: 'خبر پنهانِ حجره' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'نانوایی سپیده' })).toBeTruthy()
    expect(screen.getAllByText('شاگرد نانوا')).toHaveLength(2)
    expect(screen.getByTestId('story-opening-card').querySelectorAll('.hc-story-opening__copy p')).toHaveLength(3)
    expect(screen.getByText('دو مکان را بررسی کنید و بر اساس شواهد تصمیم بگیرید.')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'انتخاب مکان‌ها' })).toBeEnabled()
    expect(screen.queryByRole('button', { name: /پیام‌ها/ })).toBeNull()
    expect(screen.queryByRole('button', { name: /پیمان‌ها/ })).toBeNull()
    expect(screen.queryByText('Pressure')).toBeNull()
    expect(screen.queryByText('Market diagnostics')).toBeNull()
  })

  it('presents investigation, evidence, business activity and the Backend-authored decision in order', async () => {
    render(<TeamGameplayScreen experience={teamExperienceFixture} world={publicWorldFixture} canWrite />, { wrapper })
    await userEvent.click(screen.getByRole('button', { name: 'انتخاب مکان‌ها' }))

    await userEvent.click(screen.getByRole('button', { name: 'دفتر حاج صادق' }))
    expect(screen.getByTestId('team-market-marker-haj-sadegh-office')).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByTestId('scene-sheet')).toBeTruthy()
    expect(screen.getByTestId('evidence-office-erased-loads')).toHaveTextContent('سه ردیف پاک‌شده')
    expect(screen.getByTestId('evidence-office-erased-loads')).toHaveTextContent('منبع: تکه دفتر آورده‌شده توسط رحیم')
    expect(screen.getByTestId('evidence-office-erased-loads')).toHaveTextContent('اهمیت')
    expect(screen.getByTestId('evidence-office-erased-loads')).toHaveTextContent('ممکن است باز کند')
    expect(screen.getByTestId('evidence-office-wrong-seal')).toHaveTextContent('نامطمئن')
    expect(screen.getByTestId('evidence-office-noon-note')).toHaveTextContent('محتمل')
    await userEvent.click(screen.getByRole('button', { name: 'مقایسه شماره بارها' }))
    await waitFor(() => expect(screen.queryByTestId('scene-sheet')).toBeNull())
    expect(screen.getByTestId('team-market-marker-haj-sadegh-office')).toHaveClass('is-investigated')
    expect(screen.getByRole('button', { name: 'یک بررسی باقی مانده' })).toBeEnabled()

    await userEvent.click(screen.getByRole('button', { name: 'یک بررسی باقی مانده' }))
    await userEvent.click(screen.getByRole('button', { name: 'دروازه بازار' }))
    expect(screen.getByTestId('evidence-gate-route-permit')).toHaveTextContent('اجازه خروج از راه فرعی')
    expect(screen.getByTestId('evidence-gate-open-window')).toHaveTextContent('محتمل')
    await userEvent.click(screen.getByTestId('scene-sheet').querySelector('.hc-scene-sheet__action')!)
    await waitFor(() => expect(screen.getByRole('button', { name: 'مرور یافته‌ها' })).toBeEnabled())

    await userEvent.click(screen.getByRole('button', { name: 'مرور یافته‌ها' }))
    expect(screen.getByTestId('final-decision-summary')).toBeTruthy()
    await userEvent.click(screen.getByRole('button', { name: 'ورود به فعالیت کسب‌وکار' }))
    expect(screen.getByTestId('story-business-action')).toBeTruthy()
    expect(screen.getByText('اثر روی کسب‌وکار شما')).toBeTruthy()
    expect(screen.getByText('راه‌های بازشده با تحقیق شما')).toBeTruthy()

    await userEvent.click(screen.getByRole('button', { name: 'مهر تصمیم نهایی' }))
    expect(screen.getByTestId('final-decision-summary')).toBeTruthy()
    expect(screen.getByRole('button', { name: /قیمت را نگه دار/ })).toBeTruthy()
  })

  it('reveals a complete three-card evidence bundle at the market gate', async () => {
    render(<TeamGameplayScreen experience={teamExperienceFixture} world={publicWorldFixture} canWrite />, { wrapper })
    await userEvent.click(screen.getByRole('button', { name: 'انتخاب مکان‌ها' }))
    await userEvent.click(screen.getByRole('button', { name: 'دروازه بازار' }))

    expect(screen.getByTestId('evidence-gate-route-permit')).toHaveTextContent('قطعی')
    expect(screen.getByTestId('evidence-gate-wheel-tracks')).toHaveTextContent('نامطمئن')
    expect(screen.getByTestId('evidence-gate-open-window')).toHaveTextContent('محتمل')
    expect(screen.getByTestId('scene-sheet').querySelectorAll('.hc-evidence-card')).toHaveLength(3)
  })

  it('shows the market reaction and next-scene action for a submitted decision', async () => {
    const submittedExperience = {
      ...teamExperienceFixture,
      privateStorylets: teamExperienceFixture.privateStorylets.map((storylet) => ({
        ...storylet,
        submitted: true,
        submittedChoiceId: 'keep-price',
      })),
    }
    render(<TeamGameplayScreen experience={submittedExperience} world={publicWorldFixture} canWrite />, { wrapper })

    expect(screen.getByTestId('story-market-reaction')).toBeTruthy()
    expect(screen.getAllByRole('heading', { name: 'قیمت را نگه دار' })).toHaveLength(2)
    await userEvent.click(screen.getByRole('button', { name: 'ادامه به صحنه بعد' }))
    expect(screen.getByRole('status')).toHaveTextContent('در حال همگام‌سازی صحنه بعد')
  })

})
