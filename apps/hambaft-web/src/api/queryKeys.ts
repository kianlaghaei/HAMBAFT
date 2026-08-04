export const queryKeys = {
  packages: ['packages'] as const,
  package: (id: string, version: string) => ['package', id, version] as const,
  session: (id: string) => ['session', id] as const,
  admin: (id: string) => ['admin', id] as const,
  teamExperience: ['team-experience'] as const,
  publicWorld: (id: string) => ['public-world', id] as const,
  inbox: ['proposals', 'inbox'] as const,
  outbox: ['proposals', 'outbox'] as const,
  agreements: ['agreements'] as const,
  endings: ['endings'] as const,
}
