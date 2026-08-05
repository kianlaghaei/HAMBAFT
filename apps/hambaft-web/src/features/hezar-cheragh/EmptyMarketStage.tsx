import { TeamMarketMarkers } from './TeamMarketMarkers'

type EmptyMarketStageProps = {
  ownedLocationId?: string
  activeLocationId?: string
  selectedLocationId?: string
  urgentLocationIds?: readonly string[]
  disabledLocationIds?: readonly string[]
  onSelectLocation?: (locationId?: string) => void
}

export function EmptyMarketStage({ ownedLocationId, activeLocationId, selectedLocationId, urgentLocationIds, disabledLocationIds, onSelectLocation }: EmptyMarketStageProps) {
  return <div className="empty-market-stage" data-testid="empty-market-stage">
    <img src="/assets/hezar-cheragh/team-market-map.webp" alt="" aria-hidden="true" data-testid="team-market-map-image" />
    <TeamMarketMarkers ownedLocationId={ownedLocationId} activeLocationId={activeLocationId} selectedLocationId={selectedLocationId} urgentLocationIds={urgentLocationIds} disabledLocationIds={disabledLocationIds} onSelectLocation={onSelectLocation} />
  </div>
}
