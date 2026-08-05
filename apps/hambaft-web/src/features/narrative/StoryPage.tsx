import { useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'motion/react'
import { api, ApiError } from '../../api/client'
import { queryKeys } from '../../api/queryKeys'
import { useAuthStore } from '../../auth/authStore'
import { EmptyState, ErrorState } from '../../components/States'
import { useTeamContext } from '../team-experience/teamContext'
import { usePublicWorld } from '../../hooks/useServerQuery'
import { TeamGameplayScreen } from '../hezar-cheragh/TeamGameplayScreen'
import { ChoiceList } from '../decisions/ChoiceList'

export function StoryPage() {
  const { experience, canWrite } = useTeamContext()
  const hasGameplayProjection = Boolean(experience.worldPresentation)
  const world = usePublicWorld(hasGameplayProjection ? experience.sessionId : undefined)
  const token = useAuthStore((state) => state.team?.accessToken ?? '')
  const client = useQueryClient()
  const mutation = useMutation({
    mutationFn: ({ assignmentId, choiceId }: { assignmentId: string; choiceId: string }) => api.submitChoice(assignmentId, choiceId, experience.stateVersion, token),
    onSuccess: () => void client.invalidateQueries({ queryKey: queryKeys.teamExperience }),
    onError: (error) => { if (error instanceof ApiError && error.isConflict) void client.invalidateQueries({ queryKey: queryKeys.teamExperience }) },
  })
  if (experience.sessionMetadata?.storyPackageId === 'hezar-cheragh' && hasGameplayProjection) {
    if (world.isLoading) return <div className="hc-play-loading" role="status">نقشه‌ی بازار در حال روشن‌شدن است…</div>
    if (world.isError || !world.data) return <ErrorState error={world.error} retry={() => void world.refetch()} />
    return <TeamGameplayScreen experience={experience} world={world.data} canWrite={canWrite} />
  }
  if (!experience.privateStorylets.length) return <EmptyState title={String(experience.sessionMetadata?.status) === 'Completed' ? 'روایت این جلسه به پایان رسیده است' : 'در انتظار فصل بعد'}>وقتی بازار برای حجره شما خبری داشته باشد، این صفحه تازه می‌شود.</EmptyState>
  return <div className="narrative-stack">{experience.privateStorylets.map((storylet) => <motion.article className="story-sheet" key={storylet.assignmentId} initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
    <div className="story-tags">{storylet.presentationTags.speaker && <span>{storylet.presentationTags.speaker}</span>}{storylet.presentationTags.mood && <span>{storylet.presentationTags.mood}</span>}</div>
    <h2>{storylet.title}</h2>
    <div className="narrative-copy">{storylet.paragraphs.map((paragraph, index) => <p key={index}>{paragraph}</p>)}</div>
    {storylet.submitted ? <div className="decision-registered" role="status">تصمیم شما ثبت شد. اگر این تصمیم نیازمند حل مشترک باشد، منتظر دیگر حجره‌ها بمانید.</div> : <ChoiceList storylet={storylet} disabled={!canWrite || mutation.isPending} onChoose={(choiceId) => mutation.mutate({ assignmentId: storylet.assignmentId, choiceId })} />}
    {mutation.error && <ErrorState error={mutation.error instanceof ApiError ? mutation.error : mutation.error} retry={() => mutation.reset()} />}
  </motion.article>)}</div>
}
