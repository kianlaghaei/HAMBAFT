import { usePublicWorld } from '../../hooks/useServerQuery'
import { controllerLabel, statusLabel } from '../../design-system/presentation'
import { ErrorState, LoadingState, Section } from '../../components/States'
import { useTeamContext } from '../team-experience/teamContext'

export function MarketPage() {
  const { experience } = useTeamContext()
  const world = usePublicWorld(experience.sessionId)
  if (world.isLoading) return <LoadingState />
  if (world.isError || !world.data) return <ErrorState error={world.error} retry={() => void world.refetch()} />
  return <Section title="بازار هزارچراغ" eyebrow="نمای عمومی کسب‌وکارها"><div className="market-grid">{world.data.entities.map((entity) => <article className="business-tile" key={entity.id}><div className="shop-mark" aria-hidden="true">✦</div><h3>{entity.displayName}</h3><p>{controllerLabel(entity.controllerType)}</p><span className="status-chip">{statusLabel(entity.status)}</span></article>)}</div></Section>
}
