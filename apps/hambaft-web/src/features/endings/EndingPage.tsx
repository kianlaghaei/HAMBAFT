import { Navigate } from 'react-router-dom'
import { EmptyState } from '../../components/States'
import { useTeamContext } from '../team-experience/teamContext'
import { EndingView } from './EndingView'
import { sessionStatusName } from '../../design-system/presentation'

export function EndingPage() {
  const { experience } = useTeamContext()
  if (sessionStatusName(experience.sessionMetadata?.status ?? '') !== 'Completed') return <Navigate to="/team" replace />
  if (!experience.entityEnding && !experience.worldEnding) return <EmptyState title="پایان در حال انتشار است">چند لحظه دیگر دوباره بررسی کنید.</EmptyState>
  return <div className="ending-stack">{experience.entityEnding && <EndingView ending={experience.entityEnding} />}{experience.worldEnding && <EndingView ending={experience.worldEnding} world />}</div>
}
