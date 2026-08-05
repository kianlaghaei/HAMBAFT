import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useUiStore } from '../../state/uiStore'
import { BazaarMapViewport } from './BazaarMapViewport'
import { GameplayPanel } from './GameplayPanel'
import { registerAssets } from './assetRegistry'
import { devAssetManifest } from './devManifest'
import { loadBazaarMap } from './bazaarMapConfig'
import type { PanelFixture, PanelMode } from './fixtures'
import type { SceneLocation } from '../visual-world/types'
import { buildSceneDescriptor } from '../visual-world/sceneDirector'
import type { PublicWorld, TeamExperience } from '../../api/schemas'

export type HezarCheraghShellProps = {
  world: PublicWorld
  team?: TeamExperience
  panelFixture: PanelFixture
  timeMode?: 'morning' | 'dusk' | 'night'
  compact?: boolean
  notification?: string
  onPanelClose?: () => void
  onPanelConfirm?: () => void
  onSelectLocation?: (location: SceneLocation) => void
  onChangePanelMode?: (mode: PanelMode) => void
  children?: ReactNode
}

export function HezarCheraghShell({
  world,
  team,
  panelFixture,
  timeMode = 'morning',
  compact = false,
  notification,
  onPanelClose,
  onPanelConfirm,
  onSelectLocation,
  onChangePanelMode,
  children,
}: HezarCheraghShellProps) {
  const reducedMotion = useUiStore((state) => state.reducedMotion)
  const [selectedLocationId, setSelectedLocationId] = useState<string>()
  const [sheetState, setSheetState] = useState<'collapsed' | 'half' | 'expanded'>('half')
  const [reactionIndex, setReactionIndex] = useState(0)

  // Register dev manifest on mount
  useEffect(() => {
    registerAssets(devAssetManifest)
  }, [])

  const descriptor = useMemo(() => buildSceneDescriptor(world, team), [world, team])
  const mapDef = useMemo(() => loadBazaarMap(), [])
  const bazaarTime = timeMode
  const selectedLocation = descriptor.locations.find((location) => location.id === selectedLocationId)
  const pactTargetIds = useMemo(
    () => new Set(panelFixture.mode === 'PactTargetSelection' ? panelFixture.availableTargets.map((target) => target.id) : []),
    [panelFixture],
  )
  const changedLocationIds = useMemo(
    () => new Set(panelFixture.mode === 'WorldReaction' ? panelFixture.reactions.map((reaction) => reaction.locationId) : []),
    [panelFixture],
  )
  const reactionLocationId = panelFixture.mode === 'WorldReaction' ? panelFixture.reactions[reactionIndex]?.locationId : undefined
  const activeFixture: PanelFixture = selectedLocation
    ? {
      mode: 'LocationContext',
      location: {
        name: selectedLocation.name,
        whyItMatters: selectedLocation.whyItMatters,
        whoIsHere: selectedLocation.whoIsHere,
        currentCondition: selectedLocation.currentCondition,
        recentChange: selectedLocation.recentChange,
        availableActions: selectedLocation.availableActions,
      },
    }
    : panelFixture

  const handleSelectLocation = useCallback((location: SceneLocation) => {
    setSelectedLocationId(location.id)
    onSelectLocation?.(location)
  }, [onSelectLocation])

  const closePanel = useCallback(() => {
    setSelectedLocationId(undefined)
    onPanelClose?.()
  }, [onPanelClose])

  const shellClass = [
    'hezar-cheragh-shell',
    compact ? 'is-compact' : '',
    `time-${bazaarTime}`,
    reducedMotion ? 'is-reduced-motion' : '',
  ].filter(Boolean).join(' ')

  return (
    <div className={shellClass} data-testid="hezar-cheragh-shell" data-map-width={mapDef.canvas.width} data-map-height={mapDef.canvas.height}>
      {/* Header */}
      <header className="hc-header">
        <div className="hc-header-left">
          <p className="brand">HAMBAFT <span>/ هزارچراغ</span></p>
          {team?.controlledEntity && (
            <h1 className="hc-team-name">{team.controlledEntity.displayName}</h1>
          )}
        </div>
        <div className="hc-header-center">
          <span className="hc-time-label">
            {bazaarTime === 'morning' ? 'صبح' : bazaarTime === 'dusk' ? 'غروب' : 'شب'}
          </span>
          <span className="hc-checkpoint">{descriptor.timeLabel}</span>
        </div>
        <div className="hc-header-right">
          {notification && (
            <div className="hc-notification" role="alert">
              {notification}
            </div>
          )}
          <span className="hc-market-state">
            بازار {descriptor.pressure === 'critical' ? 'بحرانی' : descriptor.pressure === 'strained' ? 'تحت فشار' : descriptor.pressure === 'uneasy' ? 'ناآرام' : 'آرام'}
          </span>
        </div>
      </header>

      {/* Main content: Map + Panel */}
      <div className="hc-main">
        <div className="hc-map-area">
          <BazaarMapViewport
            descriptor={descriptor}
            timeMode={bazaarTime}
            selectedLocationId={selectedLocationId}
            pactTargetIds={pactTargetIds}
            changedLocationIds={changedLocationIds}
            reactionFocusLocationId={reactionLocationId}
            onSelectLocation={handleSelectLocation}
          />
        </div>
        <div className={`hc-panel-area sheet-${sheetState}`}>
          {compact && (
            <div className="hc-sheet-controls" role="group" aria-label="اندازه پنل بازی">
              {(['collapsed', 'half', 'expanded'] as const).map((state) => (
                <button
                  key={state}
                  type="button"
                  className={sheetState === state ? 'is-active' : ''}
                  aria-pressed={sheetState === state}
                  onClick={() => setSheetState(state)}
                >
                  {state === 'collapsed' ? 'جمع‌شده' : state === 'half' ? 'نیمه' : 'باز'}
                </button>
              ))}
            </div>
          )}
          <GameplayPanel
            fixture={activeFixture}
            currentReactionIndex={reactionIndex}
            totalReactions={activeFixture.mode === 'WorldReaction' ? activeFixture.reactions.length : 0}
            onClose={closePanel}
            onConfirm={onPanelConfirm}
            onNextReaction={() => setReactionIndex((i) => i + 1)}
            onSkipReactions={closePanel}
            onSelectPactTarget={() => {
              onChangePanelMode?.('ProposalLetter')
            }}
          />
        </div>
      </div>

      {/* Footer: recent changes / business pulse */}
      <footer className="hc-footer">
        <div className="hc-footer-section">
          <span className="eyebrow">تغییرات اخیر</span>
          <ul className="hc-recent-changes">
            {descriptor.pulse.slice(0, 3).map((line, i) => (
              <li key={i}>{line}</li>
            ))}
            {descriptor.pulse.length === 0 && <li>تغییری ثبت نشده است.</li>}
          </ul>
        </div>
        <div className="hc-footer-section">
          <span className="eyebrow">نبض کسب‌وکار</span>
          <ul className="hc-business-pulse">
            {descriptor.businessPulse.slice(0, 3).map((line, i) => (
              <li key={i}>{line}</li>
            ))}
            {descriptor.businessPulse.length === 0 && <li>گزارشی موجود نیست.</li>}
          </ul>
        </div>
        <div className="hc-footer-section">
          <span className="eyebrow">پیام‌ها و پیمان‌ها</span>
          <p className="hc-pact-summary">
            {descriptor.messengers.length > 0
              ? `${descriptor.messengers.length} پیام در انتظار`
              : 'پیام جدیدی نیست'}
          </p>
        </div>
      </footer>
      {children && <div className="hc-extension-slot">{children}</div>}
    </div>
  )
}
