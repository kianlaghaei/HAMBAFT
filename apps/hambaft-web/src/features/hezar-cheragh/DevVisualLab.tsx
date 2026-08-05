import { useState, useMemo } from 'react'
import { HezarCheraghShell } from './HezarCheraghShell'
import { devFixtures, type PanelFixture, type PanelMode } from './fixtures'
import type { BazaarTimeMode } from './bazaarMapConfig'
import { useUiStore } from '../../state/uiStore'

/** Minimal mock world for the dev visual lab. */
function mockWorld(): import('../../api/schemas').PublicWorld {
  return {
    id: 'dev-session',
    stateVersion: 1,
    currentCheckpointId: 'morning-without-bell',
    narrative: { title: 'بازار هزارچراغ - آزمایشگاه بصری', presentationTags: { time: 'morning', scene: 'market-morning' } },
    worldPresentation: {
      sceneId: 'dev-scene',
      timeOfDay: 'morning',
      timeLabel: 'صبحِ بی‌زنگ',
      atmosphereLabel: 'بازار آرام',
      publicEvent: 'زنگ بازار به صدا درنیامد',
      courtyardActivity: 'بازار خلوت است',
      soundscape: 'ambient-market',
      visualTags: [],
      semanticMetrics: [{ key: 'Pressure', bandId: 'uneasy' }],
      pulse: ['زنگ بازار به صدا درنیامد', 'بار دیروز نرسید', 'دفتر حاج‌صادق بسته است'],
      ambientEvents: [{ id: 'ev1', description: 'چراغ دفتر حاج‌صادق خاموش است', locationId: 'haj-sadegh-office' }],
      characters: [
        { id: 'sepideh', displayName: 'سپیده', locationId: 'bakery-sepideh', whatIsKnown: 'نگران کمبود آرد', lastSeen: 'صبح امروز', attitude: 'نگران', recentStatement: 'اگر آرد نرسد، نان فردا را نمی‌توانم بپزم.', possibleInteraction: 'گفتگو' },
        { id: 'rah-no-manager', displayName: 'مدیر راه نو', locationId: 'logistics-rah-no', whatIsKnown: 'منتظر بازگشایی مسیر شمال', lastSeen: 'دیشب', attitude: 'صبور', recentStatement: 'تا مسیر باز نشود کاری از دستمان برنمی‌آید.', possibleInteraction: null },
      ],
      reactions: [
        { outcomeLine: 'نانوایی سپیده: سهمیه آرد کاهش یافت.', locationIds: ['bakery-sepideh'], visualTags: ['shortage'] },
        { outcomeLine: 'باربری راه نو: مسیر شمال همچنان بسته است.', locationIds: ['logistics-rah-no'], visualTags: ['blocked'] },
        { outcomeLine: 'چاپ روشن: سفارش جدید ثبت شد.', locationIds: ['printing-roshan'], visualTags: ['new-order'] },
      ],
      avanVisible: false,
      locations: [
        { id: 'market-entrance', displayName: 'دروازه بازار', shortIdentity: 'ورودی اصلی بازار', whyItMatters: 'تنها مسیر ورود بار به بازار', currentCondition: 'مسیر شمال بسته است', whoIsHere: 'نگهبان دروازه', recentChange: 'مسیر شمال از دیشب بسته شده', availableActions: ['بازرسی', 'گفتگو با نگهبان'], presentationTags: ['entry'], entityId: undefined, x: 106, y: 103 },
        { id: 'central-crossroads', displayName: 'چهارسوق', shortIdentity: 'مرکز تقاطع بازار', whyItMatters: 'محل گردهمایی‌های بازار', currentCondition: 'خلوت', whoIsHere: 'چند رهگذر', recentChange: '', availableActions: [], presentationTags: ['central'], entityId: undefined, x: 60, y: 63 },
        { id: 'clock-courtyard', displayName: 'حیاط ساعت', shortIdentity: 'حیاط مرکزی با ساعت آفتابی', whyItMatters: 'زنگ بازار از اینجا نواخته می‌شود', currentCondition: 'ساعت از کار افتاده', whoIsHere: '', recentChange: 'زنگ صبح به صدا درنیامد', availableActions: ['بررسی ساعت'], presentationTags: ['courtyard'], entityId: undefined, x: 48, y: 53 },
        { id: 'haj-sadegh-office', displayName: 'دفتر حاج‌صادق', shortIdentity: 'دفتر ریش‌سفید بازار', whyItMatters: 'تصمیم‌های کلیدی بازار اینجا گرفته می‌شود', currentCondition: 'بسته', whoIsHere: '', recentChange: 'از دیشب بسته است', availableActions: [], presentationTags: ['office'], entityId: undefined, x: 48, y: 24 },
        { id: 'bakery-sepideh', displayName: 'نانوایی سپیده', shortIdentity: 'اصلی‌ترین نانوایی بازار', whyItMatters: 'تأمین‌کننده اصلی نان بازار', currentCondition: 'فعالیت کمتر از حد معمول', whoIsHere: 'سپیده، دو کارگر', recentChange: 'سهمیه آرد نصف شد', availableActions: ['بازرسی حجره', 'گفتگو با سپیده'], presentationTags: ['bakery'], entityId: 'bakery-sepideh', entityDefinitionId: 'bakery-sepideh', x: 21, y: 33 },
        { id: 'logistics-rah-no', displayName: 'باربری راه نو', shortIdentity: 'مسئول حمل بار در بازار', whyItMatters: 'تنها باربری فعال بازار', currentCondition: 'دفتر نیمه‌باز', whoIsHere: 'مدیر راه نو', recentChange: 'مسیر شمال بسته شد', availableActions: ['بررسی مسیرها'], presentationTags: ['logistics'], entityId: 'logistics-rah-no', entityDefinitionId: 'logistics-rah-no', x: 82, y: 36 },
        { id: 'printing-roshan', displayName: 'چاپ روشن', shortIdentity: 'چاپخانه بازار', whyItMatters: 'مسئول ثبت و چاپ اسناد بازار', currentCondition: 'فعال', whoIsHere: 'مدیر چاپخانه', recentChange: 'سفارش جدید ثبت شد', availableActions: ['بررسی مدارک'], presentationTags: ['printing'], entityId: 'printing-roshan', entityDefinitionId: 'printing-roshan', x: 24, y: 76 },
        { id: 'exchange-mizan', displayName: 'صرافی میزان', shortIdentity: 'صرافی اصلی بازار', whyItMatters: 'تعیین‌کننده نرخ‌های بازار', currentCondition: 'فعال', whoIsHere: 'صراف', recentChange: '', availableActions: ['مشاهده نرخ‌ها'], presentationTags: ['exchange'], entityId: 'exchange-mizan', entityDefinitionId: 'exchange-mizan', x: 80, y: 74 },
      ],
      semantic: true,
    },
    worldEnding: false,
    entities: [
      { id: 'bakery-sepideh', definitionId: 'bakery-sepideh', displayName: 'نانوایی سپیده', status: 'Active', controllerType: 'HumanTeam', controlledByTeamId: 'team-aleph' },
      { id: 'logistics-rah-no', definitionId: 'logistics-rah-no', displayName: 'باربری راه نو', status: 'Active', controllerType: 'HumanTeam', controlledByTeamId: 'team-bet' },
      { id: 'printing-roshan', definitionId: 'printing-roshan', displayName: 'چاپ روشن', status: 'Active', controllerType: 'HumanTeam', controlledByTeamId: 'team-gimel' },
      { id: 'exchange-mizan', definitionId: 'exchange-mizan', displayName: 'صرافی میزان', status: 'Active', controllerType: 'AuthoredBehavior', controlledByTeamId: undefined },
    ],
    publicAgreements: [],
  } as unknown as import('../../api/schemas').PublicWorld
}

const modes: Array<{ label: string; key: PanelMode; fixtureKey: string }> = [
  { label: 'معرفی', key: 'SceneIntroduction', fixtureKey: 'introduction' },
  { label: 'بررسی', key: 'Investigation', fixtureKey: 'investigation' },
  { label: 'نانوایی', key: 'BusinessActivity', fixtureKey: 'bakery' },
  { label: 'باربری', key: 'BusinessActivity', fixtureKey: 'logistics' },
  { label: 'چاپ', key: 'BusinessActivity', fixtureKey: 'printing' },
  { label: 'صرافی', key: 'BusinessActivity', fixtureKey: 'exchange' },
  { label: 'هدف پیمان', key: 'PactTargetSelection', fixtureKey: 'pactTarget' },
  { label: 'نامه پیشنهاد', key: 'ProposalLetter', fixtureKey: 'proposal' },
  { label: 'پیشنهاد متقابل', key: 'Counterproposal', fixtureKey: 'counterproposal' },
  { label: 'پیمان‌ها', key: 'CompositeCommitment', fixtureKey: 'compositeCommitment' },
  { label: 'واکنش بازار', key: 'WorldReaction', fixtureKey: 'worldReaction' },
  { label: 'در انتظار', key: 'WaitingForOtherTeams', fixtureKey: 'waiting' },
  { label: 'پایان', key: 'EntityEnding', fixtureKey: 'entityEnding' },
]

const timeModes: Array<{ label: string; value: BazaarTimeMode }> = [
  { label: 'صبح', value: 'morning' },
  { label: 'غروب', value: 'dusk' },
  { label: 'شب', value: 'night' },
]

const layoutModes: Array<{ label: string; value: 'desktop' | 'tablet' | 'fallback' | 'reduced' }> = [
  { label: 'دسکتاپ', value: 'desktop' },
  { label: 'تبلت', value: 'tablet' },
  { label: 'ساده', value: 'fallback' },
  { label: 'کم‌تحرک', value: 'reduced' },
]

export function DevVisualLab() {
  const [activeMode, setActiveMode] = useState<PanelMode>('SceneIntroduction')
  const [activeFixtureKey, setActiveFixtureKey] = useState('introduction')
  const [timeMode, setTimeMode] = useState<BazaarTimeMode>('morning')
  const [layoutMode, setLayoutMode] = useState<'desktop' | 'tablet' | 'fallback' | 'reduced'>('desktop')
  const setReducedMotion = useUiStore((state) => state.setReducedMotion)
  const setVisualMode = useUiStore((state) => state.setVisualMode)
  const world = useMemo(() => mockWorld(), [])

  const fixture: PanelFixture = devFixtures[activeFixtureKey as keyof typeof devFixtures] ?? devFixtures.introduction

  const handleModeChange = (label: string, key: PanelMode, fKey: string) => {
    // Find the correct fixture key for business activities
    if (key === 'BusinessActivity') {
      const activityMap: Record<string, string> = {
        'نانوایی': 'bakery',
        'باربری': 'logistics',
        'چاپ': 'printing',
        'صرافی': 'exchange',
      }
      setActiveFixtureKey(activityMap[label] ?? fKey)
    } else {
      setActiveFixtureKey(fKey)
    }
    setActiveMode(key)
  }

  return (
    <div className={`dev-visual-lab layout-${layoutMode}`} data-testid="dev-visual-lab">
      {/* Dev toolbar */}
      <div className="dev-toolbar" role="toolbar" aria-label="کنترل‌های آزمایشگاه بصری">
        <fieldset>
          <legend>حالت پنل</legend>
          <div className="dev-mode-buttons">
            {modes.map((m) => (
              <button
                key={`${m.key}-${m.fixtureKey}`}
                type="button"
                className={`dev-mode-btn${activeMode === m.key && activeFixtureKey === m.fixtureKey ? ' is-active' : ''}`}
                onClick={() => handleModeChange(m.label, m.key, m.fixtureKey)}
              >
                {m.label}
              </button>
            ))}
          </div>
        </fieldset>
        <fieldset>
          <legend>زمان</legend>
          <div className="dev-mode-buttons">
            {timeModes.map((t) => (
              <button
                key={t.value}
                type="button"
                className={`dev-mode-btn${timeMode === t.value ? ' is-active' : ''}`}
                onClick={() => setTimeMode(t.value)}
              >
                {t.label}
              </button>
            ))}
          </div>
        </fieldset>
        <fieldset>
          <legend>چیدمان</legend>
          <div className="dev-mode-buttons">
            {layoutModes.map((l) => (
              <button
                key={l.value}
                type="button"
                className={`dev-mode-btn${layoutMode === l.value ? ' is-active' : ''}`}
              onClick={() => {
                setLayoutMode(l.value)
                if (l.value === 'fallback') {
                  setVisualMode('fallback')
                  setReducedMotion(false)
                } else if (l.value === 'reduced') {
                  setVisualMode('reduced')
                  setReducedMotion(true)
                } else {
                  setVisualMode('high')
                  setReducedMotion(false)
                }
              }}
              >
                {l.label}
              </button>
            ))}
          </div>
        </fieldset>
      </div>

      {/* Shell preview */}
      <div className="dev-preview">
        <HezarCheraghShell
          world={world}
          panelFixture={fixture}
          timeMode={timeMode}
          compact={layoutMode === 'tablet'}
          notification={activeMode === 'WaitingForOtherTeams' ? 'منتظر پاسخ تیم‌های دیگر' : undefined}
          onPanelClose={() => setActiveMode('SceneIntroduction')}
          onPanelConfirm={() => { /* no-op in dev lab */ }}
          onChangePanelMode={(mode) => {
            setActiveMode(mode)
            if (mode === 'ProposalLetter') setActiveFixtureKey('proposal')
          }}
        />
      </div>
    </div>
  )
}
