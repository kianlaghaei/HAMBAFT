import { useEffect, useState } from 'react'

export type TeamMarketMarkerDefinition = {
  id: string
  label: string
  x: number
  y: number
  businessIdentity?: string | null
}

type TeamMarketMarkersProps = {
  markers: readonly TeamMarketMarkerDefinition[]
  ownedLocationId?: string
  activeLocationId?: string
  selectedLocationId?: string
  urgentLocationIds?: readonly string[]
  disabledLocationIds?: readonly string[]
  investigatedLocationIds?: readonly string[]
  onSelectLocation?: (locationId?: string) => void
}

export function TeamMarketMarkers({ markers, ownedLocationId, activeLocationId, selectedLocationId, urgentLocationIds = [], disabledLocationIds = [], investigatedLocationIds = [], onSelectLocation }: TeamMarketMarkersProps) {
  const [localSelectedLocationId, setLocalSelectedLocationId] = useState<string>()

  useEffect(() => {
    setLocalSelectedLocationId(undefined)
  }, [activeLocationId])

  return (
    <div className="team-market-markers" data-testid="team-market-markers" aria-label="مکان‌های بازار">
      {markers.map((marker) => {
        const currentSelectedLocationId = selectedLocationId ?? localSelectedLocationId
        const isOwned = ownedLocationId === marker.id
        const isAction = activeLocationId === marker.id
        const isSelected = currentSelectedLocationId === marker.id
        const isUrgent = isAction || urgentLocationIds.includes(marker.id)
        const isDisabled = disabledLocationIds.includes(marker.id)
        const isInvestigated = investigatedLocationIds.includes(marker.id)
        const stateClass = [
          isOwned ? 'is-owned' : '',
          isAction ? 'is-action' : '',
          isSelected ? 'is-selected' : '',
          isUrgent ? 'is-urgent' : '',
          isDisabled ? 'is-disabled' : '',
          isInvestigated ? 'is-investigated' : '',
        ].filter(Boolean).join(' ')

        return (
          <button
            key={marker.id}
            type="button"
            className={`team-market-marker ${stateClass}`.trim()}
            style={{ left: `${marker.x}%`, top: `${marker.y}%` }}
            aria-label={marker.label}
            aria-pressed={isSelected}
            aria-current={isAction ? 'location' : undefined}
            aria-disabled={isDisabled}
            data-testid={`team-market-marker-${marker.id}`}
            onClick={() => {
              if (isDisabled) return
              const nextSelectedLocationId = isSelected ? undefined : marker.id
              setLocalSelectedLocationId(nextSelectedLocationId)
              onSelectLocation?.(nextSelectedLocationId)
            }}
          >
            <span className="team-market-marker__seal" aria-hidden="true" />
            {isOwned && marker.businessIdentity && <span className="team-market-marker__business-icon" aria-hidden="true">{marker.businessIdentity}</span>}
            {isOwned && <span className="team-market-marker__owner-label">حجره شما</span>}
            {isAction && <span className="team-market-marker__action-label">نشانه تازه</span>}
            <span className="team-market-marker__label" aria-hidden="true">{marker.label}</span>
          </button>
        )
      })}
    </div>
  )
}
