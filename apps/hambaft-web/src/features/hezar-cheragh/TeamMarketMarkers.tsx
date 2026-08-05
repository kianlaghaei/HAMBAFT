import { useEffect, useState } from 'react'

type TeamMarketMarkerDefinition = {
  id: string
  label: string
  x: number
  y: number
}

export const TEAM_MARKET_MARKERS: TeamMarketMarkerDefinition[] = [
  { id: 'haj-sadegh-office', label: 'دفتر حاج صادق', x: 50, y: 15 },
  { id: 'bakery-sepideh', label: 'نانوایی سپیده', x: 27, y: 28 },
  { id: 'logistics-rah-no', label: 'باربری راه‌نو', x: 25, y: 57 },
  { id: 'printing-roshan', label: 'چاپخانه روشن', x: 74, y: 29 },
  { id: 'exchange-mizan', label: 'صرافی میزان', x: 75, y: 57 },
  { id: 'market-entrance', label: 'دروازه بازار', x: 50, y: 78 },
]

type TeamMarketMarkersProps = {
  ownedLocationId?: string
  activeLocationId?: string
  selectedLocationId?: string
  urgentLocationIds?: readonly string[]
  disabledLocationIds?: readonly string[]
  onSelectLocation?: (locationId?: string) => void
}

export function TeamMarketMarkers({ ownedLocationId, activeLocationId, selectedLocationId, urgentLocationIds = [], disabledLocationIds = [], onSelectLocation }: TeamMarketMarkersProps) {
  const [localSelectedLocationId, setLocalSelectedLocationId] = useState<string>()

  useEffect(() => {
    setLocalSelectedLocationId(undefined)
  }, [activeLocationId])

  return (
    <div className="team-market-markers" data-testid="team-market-markers" aria-label="مکان‌های بازار">
      {TEAM_MARKET_MARKERS.map((marker) => {
        const currentSelectedLocationId = selectedLocationId ?? localSelectedLocationId
        const isOwned = ownedLocationId === marker.id
        const isAction = activeLocationId === marker.id
        const isSelected = currentSelectedLocationId === marker.id
        const isUrgent = isAction || urgentLocationIds.includes(marker.id)
        const isDisabled = disabledLocationIds.includes(marker.id)
        const stateClass = [
          isOwned ? 'is-owned' : '',
          isAction ? 'is-action' : '',
          isSelected ? 'is-selected' : '',
          isUrgent ? 'is-urgent' : '',
          isDisabled ? 'is-disabled' : '',
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
            {isOwned && <span className="team-market-marker__business-icon" aria-hidden="true">نان</span>}
            {isOwned && <span className="team-market-marker__owner-label">حجره شما</span>}
            {isAction && <span className="team-market-marker__action-label">نشانه تازه</span>}
            <span className="team-market-marker__label" aria-hidden="true">{marker.label}</span>
          </button>
        )
      })}
    </div>
  )
}
