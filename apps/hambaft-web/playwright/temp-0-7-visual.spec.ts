import { expect, test, type APIRequestContext, type Browser, type Page } from '@playwright/test'
import { mkdirSync } from 'node:fs'

const apiBase = `http://127.0.0.1:${process.env.HAMBAFT_E2E_API_PORT ?? '5297'}`
const outputDir = 'artifacts/visual-qa/hezar-cheragh-0.7.0'
const businesses = [
  ['bakery-sepideh', 'Bakery'],
  ['logistics-rah-no', 'Logistics'],
  ['printing-roshan', 'Printing'],
  ['exchange-mizan', 'Exchange'],
] as const

async function json<T>(response: Awaited<ReturnType<APIRequestContext['post']>>): Promise<T> {
  expect(response.ok(), await response.text()).toBe(true)
  return await response.json() as T
}

async function setupSession(request: APIRequestContext) {
  const catalog = await json<Array<{ id: string; version: string; contentHash: string }>>(await request.get(`${apiBase}/api/story-packages`))
  const metadata = catalog.find((item) => item.id === 'hezar-cheragh' && item.version === '0.7.0')
  expect(metadata).toBeTruthy()
  const created = await json<{ sessionId: string; stateVersion: number }>(await request.post(`${apiBase}/api/sessions`, { data: { storyPackageId: metadata!.id, storyVersion: metadata!.version, contentHash: metadata!.contentHash, seed: 7300, expectedVersion: 0, difficultyId: 'standard' } }))
  const adminToken = await json<{ accessToken: string }>(await request.post(`${apiBase}/internal/auth/admin`, { data: { sessionId: created.sessionId } }))
  let version = created.stateVersion
  const teams: Array<{ teamId: string; pairingCode: string; token: string }> = []
  for (const [, name] of businesses) {
    const team = await json<{ stateVersion: number; resourceId: string; pairingCode: string }>(await request.post(`${apiBase}/api/sessions/${created.sessionId}/teams`, { headers: { Authorization: `Bearer ${adminToken.accessToken}` }, data: { displayName: name, expectedVersion: version } }))
    version = team.stateVersion
    const paired = await json<{ accessToken: string }>(await request.post(`${apiBase}/api/sessions/${created.sessionId}/pair`, { data: { teamId: team.resourceId, pairingCode: team.pairingCode } }))
    teams.push({ teamId: team.resourceId, pairingCode: team.pairingCode, token: paired.accessToken })
  }
  const packageData = await json<{ entities: Array<{ id: string; displayName: string }> }>(await request.get(`${apiBase}/api/story-packages/hezar-cheragh/0.7.0`))
  const entityIds = new Map<string, string>()
  for (const entity of packageData.entities) {
    const createdEntity = await json<{ stateVersion: number; resourceId: string }>(await request.post(`${apiBase}/api/sessions/${created.sessionId}/entities`, { headers: { Authorization: `Bearer ${adminToken.accessToken}` }, data: { definitionId: entity.id, displayName: entity.displayName, controllerType: 'HumanTeam', behaviorProfileId: null, expectedVersion: version } }))
    version = createdEntity.stateVersion
    entityIds.set(entity.id, createdEntity.resourceId)
  }
  for (let index = 0; index < businesses.length; index += 1) {
    const [definitionId] = businesses[index]
    const assigned = await json<{ stateVersion: number }>(await request.post(`${apiBase}/api/sessions/${created.sessionId}/assignments`, { headers: { Authorization: `Bearer ${adminToken.accessToken}` }, data: { entityId: entityIds.get(definitionId), teamId: teams[index].teamId, expectedVersion: version } }))
    version = assigned.stateVersion
  }
  const started = await json<{ stateVersion: number }>(await request.post(`${apiBase}/api/sessions/${created.sessionId}/start`, { headers: { Authorization: `Bearer ${adminToken.accessToken}` }, data: { expectedVersion: version } }))
  version = started.stateVersion
  const initialized = await json<{ stateVersion: number }>(await request.post(`${apiBase}/api/sessions/${created.sessionId}/narrative/initialize`, { headers: { Authorization: `Bearer ${adminToken.accessToken}` }, data: { expectedStateVersion: version, commandId: crypto.randomUUID() } }))
  version = initialized.stateVersion
  await json(await request.post(`${apiBase}/api/sessions/${created.sessionId}/narrative/resolve`, { headers: { Authorization: `Bearer ${adminToken.accessToken}` }, data: { expectedStateVersion: version, commandId: crypto.randomUUID() } }))
  return { sessionId: created.sessionId, teams }
}

async function installCredential(page: Page, token: string, sessionId: string) {
  const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64url').toString('utf8')) as { team_id: string }
  await page.addInitScript(({ tokenValue, sid, tid }) => {
    const part = tokenValue.split('.')[1]
    const decoded = JSON.parse(atob(part.replace(/-/g, '+').replace(/_/g, '/')))
    const credential = { accessToken: tokenValue, tokenType: 'Bearer', expiresAtUtc: new Date(Date.now() + 3600000).toISOString(), role: 'Team', sessionId: sid, teamId: tid }
    sessionStorage.setItem('hambaft-auth', JSON.stringify({ state: { team: credential, admin: null, displays: {} }, version: 0 }))
    void decoded
  }, { tokenValue: token, sid: sessionId, tid: payload.team_id })
}

async function addOverlay(page: Page) {
  await page.evaluate(() => {
    const root = document.querySelector('.team-market-markers')
    if (!root) return
    root.querySelectorAll('.qa-hotspot-index').forEach((node) => node.remove())
    root.querySelectorAll('.team-market-marker').forEach((marker, index) => {
      const label = document.createElement('span')
      label.className = 'qa-hotspot-index'
      label.textContent = String(index + 1)
      Object.assign(label.style, { position: 'absolute', insetInlineStart: '50%', insetBlockStart: '-22px', transform: 'translateX(-50%)', background: '#f7c65a', color: '#241b13', border: '2px solid #241b13', borderRadius: '50%', width: '24px', height: '24px', display: 'grid', placeItems: 'center', font: '700 14px sans-serif', zIndex: '8' })
      marker.append(label)
    })
  })
}

test('0.7.0 authored scene hotspot visual QA', async ({ browser, request }) => {
  mkdirSync(outputDir, { recursive: true })
  const setup = await setupSession(request)
  const teams = setup.teams
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'fa-IR' })
  for (let index = 0; index < teams.length; index += 1) {
    const page = await context.newPage()
    await installCredential(page, teams[index].token, setup.sessionId)
    await page.goto('/team/story')
    await expect(page.getByTestId('team-gameplay-screen')).toBeVisible({ timeout: 30000 })
    await expect(page.getByTestId('team-scene-background')).toBeVisible({ timeout: 30000 })
    await expect(page.locator('.team-market-marker')).toHaveCount(2)
    await addOverlay(page)
    await page.screenshot({ path: `${outputDir}/${businesses[index][0]}.png`, fullPage: false })
    await page.close()
  }
  const shared = await context.newPage()
  await installCredential(shared, teams[0].token, setup.sessionId)
  await shared.goto('/team/story')
  await expect(shared.getByTestId('team-gameplay-screen')).toBeVisible({ timeout: 30000 })
  await expect(shared.locator('.team-market-marker')).toHaveCount(2)
  await addOverlay(shared)
  await shared.screenshot({ path: `${outputDir}/shared-market.png`, fullPage: false })
  await shared.close()
  await context.close()
})
