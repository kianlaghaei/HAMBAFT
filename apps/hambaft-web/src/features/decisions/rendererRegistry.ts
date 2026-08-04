import type { TermSchema } from '../../api/schemas'

export const gameplayRendererCapabilities = [
  'single-choice', 'multiple-choice', 'ranked-choice', 'numeric-allocation', 'numeric-range', 'short-declaration',
  'compound-decision', 'target-selection', 'document-selection', 'proposal-term-form',
] as const

export type GameplayRendererKind = typeof gameplayRendererCapabilities[number]

export function rendererForStorylet(tags: Record<string, string>): GameplayRendererKind {
  const requested = tags.renderer?.toLowerCase().replace(/_/g, '-') as GameplayRendererKind | undefined
  return requested && gameplayRendererCapabilities.includes(requested) ? requested : 'single-choice'
}

export function rendererForTerm(schema: TermSchema): GameplayRendererKind {
  return ({
    Numeric: 'numeric-range', NumericRange: 'numeric-range', NumericAllocation: 'numeric-allocation',
    ShortText: 'short-declaration', ShortDeclaration: 'short-declaration', Compound: 'compound-decision', CompoundDecision: 'compound-decision',
    SingleChoice: 'single-choice', MultipleChoice: 'multiple-choice', RankedChoice: 'ranked-choice',
    TargetSelection: 'target-selection', DocumentSelection: 'document-selection', EvidenceSelection: 'document-selection',
  } as Record<string, GameplayRendererKind>)[schema.type] ?? 'proposal-term-form'
}
