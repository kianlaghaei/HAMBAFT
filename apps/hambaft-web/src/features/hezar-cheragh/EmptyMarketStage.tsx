import { TeamMarketMarkers, type TeamMarketMarkerDefinition } from './TeamMarketMarkers'

type EmptyMarketStageProps = {
  backgroundSrc?: string
  markers: readonly TeamMarketMarkerDefinition[]
  ownedLocationId?: string
  activeLocationId?: string
  selectedLocationId?: string
  urgentLocationIds?: readonly string[]
  disabledLocationIds?: readonly string[]
  investigatedLocationIds?: readonly string[]
  onSelectLocation?: (locationId?: string) => void
}

export function EmptyMarketStage({ backgroundSrc, markers, ownedLocationId, activeLocationId, selectedLocationId, urgentLocationIds, disabledLocationIds, investigatedLocationIds, onSelectLocation }: EmptyMarketStageProps) {
  return <div className="empty-market-stage" data-testid="empty-market-stage">
    {backgroundSrc && <img src={backgroundSrc} alt="" aria-hidden="true" data-testid="team-market-map-image" />}
    <TeamMarketMarkers markers={markers} ownedLocationId={ownedLocationId} activeLocationId={activeLocationId} selectedLocationId={selectedLocationId} urgentLocationIds={urgentLocationIds} disabledLocationIds={disabledLocationIds} investigatedLocationIds={investigatedLocationIds} onSelectLocation={onSelectLocation} />
  </div>
}
