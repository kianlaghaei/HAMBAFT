import type { ZodType } from 'zod'
import {
  adminSchema, commandSchema, packageCatalogSchema, packageSchema, publicWorldSchema,
  sessionSchema, teamExperienceSchema, tokenSchema,
  type AdminSession, type CommandResponse, type PackageCatalog, type PublicWorld, type Session, type StoryPackage, type TeamExperience, type Token,
} from './schemas'

const apiBase = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly title: string,
    public readonly detail: string,
    public readonly correlationId?: string,
  ) { super(detail); this.name = 'ApiError' }

  get isConflict() { return this.status === 409 }
}

type RequestOptions = Omit<RequestInit, 'body'> & { body?: unknown; token?: string; schema?: ZodType }

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  headers.set('Accept-Language', import.meta.env.VITE_DEFAULT_LOCALE ?? 'fa-IR')
  if (options.body !== undefined) headers.set('Content-Type', 'application/json')
  if (options.token) headers.set('Authorization', `Bearer ${options.token}`)
  let response: Response
  try {
    response = await fetch(`${apiBase}${path}`, { ...options, headers, body: options.body === undefined ? undefined : JSON.stringify(options.body) })
  } catch {
    throw new ApiError(0, 'Backend unavailable', 'ارتباط با سرور برقرار نشد. وضعیت سرویس را بررسی و دوباره تلاش کنید.')
  }
  if (!response.ok) {
    const fallback = response.status === 409
      ? 'بازی هنگام ارسال این اقدام تغییر کرد. وضعیت فعلی را تازه‌سازی و دوباره تلاش کنید.'
      : response.status === 401 ? 'اطلاعات ورود معتبر نیست یا منقضی شده است.'
      : response.status === 403 ? 'اجازه انجام این کار را ندارید.'
      : response.status === 404 ? 'مورد درخواستی پیدا نشد.'
      : 'سرور نتوانست این درخواست را انجام دهد.'
    let problem: { title?: string; detail?: string } = {}
    try { problem = await response.json() as { title?: string; detail?: string } } catch { /* non-JSON boundary */ }
    throw new ApiError(response.status, problem.title ?? 'خطای درخواست', response.status === 409 ? fallback : (problem.detail ?? fallback), response.headers.get('X-Correlation-ID') ?? undefined)
  }
  if (response.status === 204) return undefined as T
  const data: unknown = await response.json()
  if (!options.schema) return data as T
  const parsed = options.schema.safeParse(data)
  if (!parsed.success) throw new ApiError(502, 'Invalid API contract', 'پاسخ سرور با قرارداد مورد انتظار هماهنگ نیست.')
  return parsed.data as T
}

export const api = {
  health: () => fetch(`${apiBase}/health`),
  packages: (): Promise<PackageCatalog> => request('/api/story-packages', { schema: packageCatalogSchema }),
  package: (id: string, version: string): Promise<StoryPackage> => request(`/api/story-packages/${encodeURIComponent(id)}/${encodeURIComponent(version)}`, { schema: packageSchema }),
  createSession: (body: object): Promise<CommandResponse> => request('/api/sessions', { method: 'POST', body, schema: commandSchema }),
  session: (id: string): Promise<Session> => request(`/api/sessions/${id}`, { schema: sessionSchema }),
  addTeam: (id: string, body: object): Promise<CommandResponse> => request(`/api/sessions/${id}/teams`, { method: 'POST', body, schema: commandSchema }),
  createEntity: (id: string, body: object): Promise<CommandResponse> => request(`/api/sessions/${id}/entities`, { method: 'POST', body, schema: commandSchema }),
  assignEntity: (id: string, body: object): Promise<CommandResponse> => request(`/api/sessions/${id}/assignments`, { method: 'POST', body, schema: commandSchema }),
  transition: (id: string, action: 'start' | 'pause' | 'resume', version: number): Promise<CommandResponse> => request(`/api/sessions/${id}/${action}`, { method: 'POST', body: { expectedVersion: version }, schema: commandSchema }),
  pair: (pairingCode: string): Promise<Token> => request('/api/pair', { method: 'POST', body: { pairingCode }, schema: tokenSchema }),
  devToken: (role: 'admin' | 'public-display', sessionId: string): Promise<Token> => request(`/internal/auth/${role}`, { method: 'POST', body: { sessionId }, schema: tokenSchema }),
  teamExperience: (token: string): Promise<TeamExperience> => request('/api/story/experience', { token, schema: teamExperienceSchema }),
  publicWorld: (id: string): Promise<PublicWorld> => request(`/api/public/sessions/${id}`, { schema: publicWorldSchema }),
  admin: (id: string, token: string): Promise<AdminSession> => request(`/api/admin/sessions/${id}`, { token, schema: adminSchema }),
  initialize: (id: string, version: number, token: string): Promise<CommandResponse> => request(`/api/sessions/${id}/narrative/initialize`, { method: 'POST', token, body: { expectedStateVersion: version, commandId: crypto.randomUUID() }, schema: commandSchema }),
  submitChoice: (assignmentId: string, choiceId: string, expectedStateVersion: number, token: string): Promise<CommandResponse> => request('/api/story/choices', { method: 'POST', token, body: { assignmentId, choiceId, expectedStateVersion, commandId: crypto.randomUUID() }, schema: commandSchema }),
  sendProposal: (body: object, token: string): Promise<CommandResponse> => request('/api/proposals', { method: 'POST', token, body, schema: commandSchema }),
  proposalAction: (proposalId: string, action: 'counter' | 'accept' | 'reject' | 'cancel', body: object, token: string): Promise<CommandResponse> => request(`/api/proposals/${proposalId}/${action}`, { method: 'POST', token, body, schema: commandSchema }),
  resolveCheckpoint: (id: string, version: number, token: string): Promise<CommandResponse> => request(`/api/sessions/${id}/narrative/resolve`, { method: 'POST', token, body: { expectedStateVersion: version, commandId: crypto.randomUUID() }, schema: commandSchema }),
  resolveEndings: (id: string, version: number, token: string): Promise<CommandResponse> => request(`/api/sessions/${id}/endings/resolve`, { method: 'POST', token, body: { expectedStateVersion: version, commandId: crypto.randomUUID() }, schema: commandSchema }),
}
