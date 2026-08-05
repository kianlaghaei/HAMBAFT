import { AssetId } from './assetRegistry'

export type BazaarMapDefinition = {
  canvas: { width: number; height: number }
  locations: Array<{
    id: string
    assetId: string
    x: number
    y: number
    scale: number
    anchorX: number
    anchorY: number
    zIndex: number
    labelPosition: { x: number; y: number }
  }>
}

export type BazaarTimeMode = 'morning' | 'dusk' | 'night'

/** Provisional map definition using currently approved asset IDs.
 *  Final coordinates remain easy to replace after the production map is delivered.
 */
export const defaultBazaarMap: BazaarMapDefinition = {
  canvas: { width: 1200, height: 820 },
  locations: [
    {
      id: 'market-entrance',
      assetId: AssetId.locationGate,
      x: 1060,
      y: 720,
      scale: 0.9,
      anchorX: 0.5,
      anchorY: 0.8,
      zIndex: 3,
      labelPosition: { x: 1060, y: 750 },
    },
    {
      id: 'central-crossroads',
      assetId: AssetId.locationChaharsoq,
      x: 600,
      y: 440,
      scale: 1.1,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 2,
      labelPosition: { x: 600, y: 505 },
    },
    {
      id: 'clock-courtyard',
      assetId: AssetId.bazaarBase,
      x: 480,
      y: 370,
      scale: 1.0,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 1,
      labelPosition: { x: 480, y: 440 },
    },
    {
      id: 'haj-sadegh-office',
      assetId: AssetId.locationHajSadeghOffice,
      x: 480,
      y: 170,
      scale: 1.05,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 4,
      labelPosition: { x: 480, y: 235 },
    },
    {
      id: 'bakery-sepideh',
      assetId: AssetId.businessBakery,
      x: 210,
      y: 230,
      scale: 0.95,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 5,
      labelPosition: { x: 210, y: 295 },
    },
    {
      id: 'logistics-rah-no',
      assetId: AssetId.businessLogistics,
      x: 820,
      y: 250,
      scale: 0.95,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 5,
      labelPosition: { x: 820, y: 315 },
    },
    {
      id: 'printing-roshan',
      assetId: AssetId.businessPrinting,
      x: 240,
      y: 530,
      scale: 0.95,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 5,
      labelPosition: { x: 240, y: 595 },
    },
    {
      id: 'exchange-mizan',
      assetId: AssetId.businessExchange,
      x: 800,
      y: 520,
      scale: 0.95,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 5,
      labelPosition: { x: 800, y: 585 },
    },
    {
      id: 'caravanserai',
      assetId: AssetId.locationCaravanserai,
      x: 1060,
      y: 380,
      scale: 0.85,
      anchorX: 0.5,
      anchorY: 0.5,
      zIndex: 3,
      labelPosition: { x: 1060, y: 445 },
    },
  ],
}

/** Load a map definition from a configuration source.
 *  Always returns the default map; override point for future dynamic loading. */
export function loadBazaarMap(): BazaarMapDefinition {
  return defaultBazaarMap
}
