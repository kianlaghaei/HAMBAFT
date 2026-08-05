import { lazy, Suspense, useCallback, useMemo, useState, type CSSProperties } from 'react'
import type { SceneDescriptor, SceneLocation, VisualMode } from '../visual-world/types'
import { FallbackMarket } from '../visual-world/FallbackMarket'
import { useUiStore } from '../../state/uiStore'
import type { BazaarTimeMode } from './bazaarMapConfig'

const PixiMarketCanvas = lazy(() => import('../visual-world/PixiMarketCanvas').then((m) => ({ default: m.PixiMarketCanvas })))

export type MapLocationState = 'idle' | 'selected' | 'focused' | 'new-information' | 'action-available' | 'changed-since-last-view' | 'pact-target'

export type BazaarMapViewportProps = {
  descriptor: SceneDescriptor
  timeMode: BazaarTimeMode
  visualMode?: VisualMode
  selectedLocationId?: string
  focusedLocationId?: string
  pactTargetIds?: Set<string>
  reactionFocusLocationId?: string
  changedLocationIds?: Set<string>
  newInfoLocationIds?: Set<string>
  onSelectLocation?: (location: SceneLocation) => void
  onPixiFailure?: () => void
}

export function BazaarMapViewport({
  descriptor,
  timeMode,
  visualMode: preferredMode,
  selectedLocationId,
  focusedLocationId,
  pactTargetIds,
  reactionFocusLocationId,
  changedLocationIds,
  newInfoLocationIds,
  onSelectLocation,
  onPixiFailure,
}: BazaarMapViewportProps) {
  const systemReduced = useUiStore((state) => state.reducedMotion)
  const storedMode = useUiStore((state) => state.visualMode)
  const [pixiFailed, setPixiFailed] = useState(false)
  const [loading, setLoading] = useState(true)

  const actualMode = preferredMode ?? storedMode
  const mode: VisualMode = systemReduced && actualMode === 'high' ? 'reduced' : pixiFailed ? 'fallback' : actualMode

  const handleFailure = useCallback(() => {
    setPixiFailed(true)
    onPixiFailure?.()
  }, [onPixiFailure])

  const adaptedDescriptor = useMemo(() => {
    const adapted: SceneDescriptor = {
      ...descriptor,
      time: timeMode === 'dusk' ? 'dusk' : timeMode === 'night' ? 'night' : 'morning',
      locations: descriptor.locations.map((loc) => {
        let state: SceneLocation['state'] = 'quiet'
        if (loc.id === selectedLocationId) state = 'action-available'
        else if (loc.id === reactionFocusLocationId) state = 'public-event'
        else if (pactTargetIds?.has(loc.id)) state = 'proposal-received'
        else if (changedLocationIds?.has(loc.id)) state = 'changed-since-last-visit'
        else if (newInfoLocationIds?.has(loc.id)) state = 'new-information'
        return { ...loc, state }
      }),
    }
    return adapted
  }, [descriptor, timeMode, selectedLocationId, reactionFocusLocationId, pactTargetIds, changedLocationIds, newInfoLocationIds])

  const cameraStyle: CSSProperties | undefined = useMemo(() => {
    if (focusedLocationId) {
      const loc = descriptor.locations.find((l) => l.id === focusedLocationId)
      if (loc) {
        return {
          '--camera-x': `${50 - loc.x / 10}%`,
          '--camera-y': `${50 - loc.y / 7}%`,
        } as CSSProperties
      }
    }
    return undefined
  }, [descriptor.locations, focusedLocationId])

  // Simulate loading state briefly
  useMemo(() => {
    setLoading(true)
    const timer = setTimeout(() => setLoading(false), 300)
    return () => clearTimeout(timer)
  }, [descriptor.id])

  if (loading) {
    return (
      <div className="bazaar-map-viewport is-loading" data-testid="bazaar-map-loading">
        <div className="map-loading-spinner" />
        <p>در حال بارگذاری نقشه بازار...</p>
      </div>
    )
  }

  if (pixiFailed && mode === 'fallback') {
    return (
      <div className="bazaar-map-viewport is-fallback" data-testid="bazaar-map-fallback">
        <FallbackMarket descriptor={adaptedDescriptor} reduced={mode === 'reduced'} />
        {pixiFailed && <p className="map-fallback-notice" role="status">نمای ساده نقشه فعال است.</p>}
      </div>
    )
  }

  return (
    <div
      className={`bazaar-map-viewport visual-${mode} time-${timeMode}${reactionFocusLocationId ? ' is-reaction-focus' : ''}${pactTargetIds && pactTargetIds.size > 0 ? ' is-pact-target' : ''}`}
      style={cameraStyle}
      data-testid="bazaar-map-viewport"
    >
      {mode === 'high' ? (
        <Suspense fallback={<FallbackMarket descriptor={adaptedDescriptor} reduced />}>
          <PixiMarketCanvas descriptor={adaptedDescriptor} onFailure={handleFailure} />
        </Suspense>
      ) : (
        <FallbackMarket descriptor={adaptedDescriptor} reduced={mode === 'reduced'} />
      )}

      {/* Location hotspots overlay */}
      <div className="map-hotspots" aria-label="مکان‌های قابل تعامل">
        {descriptor.locations.map((location) => {
          const ls = getLocationState(location, {
            selectedId: selectedLocationId,
            focusedId: focusedLocationId,
            pactIds: pactTargetIds,
            changedIds: changedLocationIds,
            newIds: newInfoLocationIds,
          })
          return (
            <button
              key={location.id}
              type="button"
              className={`map-hotspot state-${ls}`}
              style={{ insetInlineStart: `${location.x / 10}%`, top: `${location.y / 7}%` }}
              aria-label={`${location.name}؛ ${location.currentCondition}`}
              aria-pressed={ls === 'selected'}
              onClick={() => onSelectLocation?.(location)}
            >
              <span aria-hidden="true" />
              {location.name}
            </button>
          )
        })}
      </div>

      {/* Relationship overlay */}
      {mode !== 'reduced' && descriptor.connections.length > 0 && (
        <div className="map-relationship-overlay" aria-hidden="true">
          {descriptor.connections.map((conn) => (
            <span
              key={conn.id}
              className={`relationship-indicator kind-${conn.kind}${conn.damaged ? ' is-damaged' : ''}`}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function getLocationState(
  location: SceneLocation,
  ctx: {
    selectedId?: string
    focusedId?: string
    pactIds?: Set<string>
    changedIds?: Set<string>
    newIds?: Set<string>
  },
): MapLocationState {
  if (location.id === ctx.selectedId) return 'selected'
  if (location.id === ctx.focusedId) return 'focused'
  if (ctx.pactIds?.has(location.id)) return 'pact-target'
  if (ctx.changedIds?.has(location.id)) return 'changed-since-last-view'
  if (ctx.newIds?.has(location.id)) return 'new-information'
  if (location.state === 'action-available') return 'action-available'
  return 'idle'
}

export { type SceneLocation }
