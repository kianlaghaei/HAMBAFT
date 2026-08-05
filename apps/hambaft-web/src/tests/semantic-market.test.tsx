import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { PublicWorld, TeamExperience } from '../api/schemas'
import { MarketScene } from '../features/visual-world/MarketScene'
import { ProposalTargetMap } from '../features/proposals/MessagesPage'
import { DisplayContent } from '../features/public-display/PublicDisplayPage'
import { queryKeys } from '../api/queryKeys'
import { useUiStore } from '../state/uiStore'
import { ids, publicWorldFixture, teamExperienceFixture } from './fixtures'

const locations: NonNullable<PublicWorld['worldPresentation']>['locations'] = [
  { id: 'market-entrance', displayName: 'ورودی بازار', shortIdentity: 'آغاز بازار', whyItMatters: 'خبر ورود از اینجا می‌رسد.', currentCondition: 'رفت‌وآمد آرام است.', whoIsHere: 'باربرها', recentChange: 'زنگ شنیده نشد.', availableActions: ['دیدن مسیر'], presentationTags: ['entrance'], x: 90, y: 84, entityId: null, entityDefinitionId: null },
  { id: 'central-crossroads', displayName: 'چهارراه مرکزی', shortIdentity: 'گره بازار', whyItMatters: 'پیام‌ها پخش می‌شوند.', currentCondition: 'رهگذران مکث می‌کنند.', whoIsHere: 'پیام‌رسان‌ها', recentChange: 'گفت‌وگو بیشتر شده.', availableActions: [], presentationTags: ['crossroads'], x: 50, y: 52, entityId: null, entityDefinitionId: null },
  { id: 'clock-courtyard', displayName: 'حیاط ساعت', shortIdentity: 'محل گردهمایی', whyItMatters: 'خبر عمومی اینجا وزن می‌گیرد.', currentCondition: 'چند نفر کنار حوض‌اند.', whoIsHere: 'حجره‌داران', recentChange: 'جمع کوچک‌تر شده.', availableActions: ['شنیدن خبر'], presentationTags: ['public-gathering'], x: 50, y: 55, entityId: null, entityDefinitionId: null },
  { id: 'haj-sadegh-office', displayName: 'دفتر حاج صادق', shortIdentity: 'دفتر بسته', whyItMatters: 'نبود او آغاز بحران است.', currentCondition: 'در بسته و صندلی خالی است.', whoIsHere: 'هیچ‌کس', recentChange: 'چراغ خاموش مانده.', availableActions: ['بررسی دفتر'], presentationTags: ['office-closed'], x: 50, y: 18, entityId: null, entityDefinitionId: null },
  { id: 'bakery-sepideh', displayName: 'نانوایی سپیده', shortIdentity: 'تنور بازار', whyItMatters: 'صف فشار را نشان می‌دهد.', currentCondition: 'موجودی محدود به نظر می‌رسد.', whoIsHere: 'ربابه خانم', recentChange: 'صف کوتاه‌تر شده.', availableActions: ['انتخاب گیرنده'], presentationTags: ['bakery'], x: 22, y: 32, entityId: ids.entity, entityDefinitionId: 'bakery-sepideh' },
]

function semanticWorld(version = 12): PublicWorld {
  return { ...publicWorldFixture, storyVersion: '0.3.0', stateVersion: version, worldMetrics: [], worldPresentation: {
    sceneId: 'market-opening', timeOfDay: 'morning', timeLabel: 'صبح', atmosphereLabel: 'صبحی بی‌زنگ در بازار', publicEvent: 'دفتر حاج صادق بسته مانده است.', courtyardActivity: 'حرکت به حیاط کشیده می‌شود.', soundscape: 'opening-shutters', avanVisible: false, visualTags: ['light-warm'], pulse: ['بازار زیر فشار است.', 'اعتماد عمومی شکننده شده.'], semanticMetrics: [{ key: 'Pressure', bandId: 'strained', label: 'بازار زیر فشار است.', description: 'صف‌ها کش آمده‌اند.', visualTags: ['pressure-strained'], trend: 'steady' }], locations: [...locations], businesses: [{ entityId: ids.entity, entityDefinitionId: 'bakery-sepideh', displayName: 'نانوایی سپیده', pulse: ['موجودی محدود به نظر می‌رسد.'], visualTags: ['flour-sacks-low'] }], characters: [{ id: 'robabeh', displayName: 'ربابه خانم', locationId: 'bakery-sepideh', whatIsKnown: 'حال صف را می‌شناسد.', lastSeen: 'کنار دخل', attitude: 'نگران', recentStatement: 'تنور باید روشن بماند.', possibleInteraction: 'دیدن صف', presentationTags: ['visible'] }], ambientEvents: [{ id: 'porter', description: 'باربری با گاری خالی می‌گذرد.', locationId: 'central-crossroads' }], reactions: [{ outcomeLine: 'صف نانوایی کوتاه‌تر شده است.', locationIds: ['bakery-sepideh'], visualTags: ['queue-shorter'] }],
  } }
}

function semanticTeam(): TeamExperience { return { ...teamExperienceFixture, visibleMetrics: [], worldPresentation: semanticWorld().worldPresentation, businessPresentation: { entityId: ids.entity, entityDefinitionId: 'bakery-sepideh', displayName: 'نانوایی سپیده', pulse: ['تنور روشن است، اما کیسه‌های آرد کمتر شده‌اند.'], visualTags: ['oven-warm'] }, relationshipPresentation: [] } }

describe('semantic market experience', () => {
  it('renders equivalent semantic location/pulse information in fallback without raw metrics', async () => {
    useUiStore.setState({ visualMode: 'fallback', reducedMotion: false, completedIntroductions: { [ids.session]: true }, inspectedLocations: {}, seenReactionVersions: { [ids.session]: 12 } })
    render(<MarketScene world={semanticWorld()} team={semanticTeam()} />)
    expect(screen.getAllByText('بازار زیر فشار است.').length).toBeGreaterThan(0)
    expect(screen.queryByText('67')).not.toBeInTheDocument()
    await userEvent.click(screen.getAllByRole('button', { name: /نانوایی سپیده/ })[0]!)
    expect(screen.getByRole('complementary', { name: /اطلاعات نانوایی سپیده/ })).toHaveTextContent('موجودی محدود به نظر می‌رسد')
  })

  it('uses the compact guided route in reduced-motion mode', () => {
    useUiStore.setState({ visualMode: 'high', reducedMotion: true, completedIntroductions: {}, inspectedLocations: {}, seenReactionVersions: {} })
    render(<MarketScene world={semanticWorld()} team={semanticTeam()} />)
    expect(screen.getByRole('dialog', { name: 'معرفی کوتاه بازار' })).toHaveTextContent('ورودی بازار')
    expect(screen.getByRole('button', { name: 'پایان معرفی' })).toBeInTheDocument()
    expect(document.querySelector('canvas')).not.toBeInTheDocument()
  })

  it('shows only the package-authored reaction for a newer projection', async () => {
    useUiStore.setState({ visualMode: 'fallback', reducedMotion: false, completedIntroductions: { [ids.session]: true }, inspectedLocations: {}, seenReactionVersions: { [ids.session]: 12 } })
    render(<MarketScene world={semanticWorld(13)} />)
    await waitFor(() => expect(screen.getByText('چه چیزی عوض شد؟')).toBeInTheDocument())
    expect(screen.getByText('صف نانوایی کوتاه‌تر شده است.')).toBeInTheDocument()
  })

  it('supports keyboard Proposal target selection on the spatial map', async () => {
    const onSelect = vi.fn(); const location = { ...locations[4]!, teamId: ids.otherTeam }
    render(<ProposalTargetMap locations={[location]} selectedTeamId="" onSelect={onSelect} />)
    const target = screen.getByRole('radio', { name: /نانوایی سپیده/ }); target.focus(); await userEvent.keyboard('{Enter}')
    expect(onSelect).toHaveBeenCalledWith(ids.otherTeam)
  })

  it('keeps Public Display semantic and private-number free', () => {
    const client = new QueryClient(); client.setQueryData(queryKeys.publicWorld(ids.session), { ...semanticWorld(), worldMetrics: [{ scope: 'World', scopeId: ids.session, metricKey: 'Pressure', numericValue: 67 }] })
    render(<QueryClientProvider client={client}><DisplayContent sessionId={ids.session} /></QueryClientProvider>)
    expect(screen.getAllByText('بازار زیر فشار است.').length).toBeGreaterThan(0)
    expect(screen.queryByText('67')).not.toBeInTheDocument()
  })
})
