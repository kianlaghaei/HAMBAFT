/** Semantic asset IDs used throughout the UI. Components must not import final PNG paths directly. */
export const AssetId = {
  bazaarBase: 'bazaar.base',
  locationGate: 'location.gate',
  locationChaharsoq: 'location.chaharsoq',
  locationHajSadeghOffice: 'location.hajSadeghOffice',
  businessBakery: 'business.bakery',
  businessLogistics: 'business.logistics',
  businessPrinting: 'business.printing',
  businessExchange: 'business.exchange',
  locationCaravanserai: 'location.caravanserai',
  investigationSeal: 'investigation.seal',
  investigationSealSpent: 'investigation.sealSpent',
  proposalLetter: 'proposal.letter',
  agreementActive: 'agreement.active',
  overlayTrust: 'overlay.trust',
  overlayObligation: 'overlay.obligation',
  overlayDebt: 'overlay.debt',
  endingVeil: 'ending.veil',
} as const

export type AssetId = (typeof AssetId)[keyof typeof AssetId]

export type AssetMeta = {
  src: string
  width: number
  height: number
  alpha: boolean
  preload?: 'high' | 'low'
  label: string
}

export type AssetManifest = Record<AssetId, AssetMeta>

const registry = new Map<string, AssetMeta>()
const missingWarnings = new Set<string>()

export function registerAssets(manifest: Partial<AssetManifest>): void {
  for (const [key, meta] of Object.entries(manifest) as [string, AssetMeta][]) {
    registry.set(key, meta)
  }
}

export function resolveAsset(id: AssetId | string): AssetMeta {
  const entry = registry.get(id)
  if (entry) return entry
  if (import.meta.env.DEV && !missingWarnings.has(id)) {
    missingWarnings.add(id)
    console.warn(`[AssetRegistry] Missing asset: "${id}". Using fallback.`)
  }
  return { src: '', width: 64, height: 64, alpha: false, label: `[missing: ${id}]` }
}

export function preloadAssets(ids: AssetId[]): Promise<void> {
  const promises = ids.map((id) => {
    const meta = resolveAsset(id)
    if (!meta.src) return Promise.resolve()
    return new Promise<void>((resolve) => {
      const img = new Image()
      img.onload = () => resolve()
      img.onerror = () => resolve()
      img.src = meta.src
    })
  })
  return Promise.all(promises).then(() => undefined)
}

export function getRegisteredAssetIds(): string[] {
  return Array.from(registry.keys())
}

export function clearAssetRegistry(): void {
  registry.clear()
  missingWarnings.clear()
}
