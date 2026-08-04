import { useOutletContext } from 'react-router-dom'
import type { TeamExperience } from '../../api/schemas'

export const useTeamContext = () => useOutletContext<{ experience: TeamExperience; canWrite: boolean }>()
