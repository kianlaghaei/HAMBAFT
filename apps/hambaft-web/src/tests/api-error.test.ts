import { expect, it, vi } from 'vitest'
import { request } from '../api/client'

it('maps HTTP 409 to the Persian stale-state experience without retrying', async () => {
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Conflict', detail: 'raw details' }), { status: 409, headers: { 'Content-Type': 'application/problem+json' } }))
  vi.stubGlobal('fetch', fetchMock)
  await expect(request('/api/example', { method: 'POST', body: {} })).rejects.toMatchObject({ status: 409, detail: 'بازی هنگام ارسال این اقدام تغییر کرد. وضعیت فعلی را تازه‌سازی و دوباره تلاش کنید.' })
  expect(fetchMock).toHaveBeenCalledTimes(1)
})
