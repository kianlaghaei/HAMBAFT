import { expect, test, type Page } from '@playwright/test'
import { mkdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const finalScreenshotDir = fileURLToPath(new URL('../../../artifacts/visual-review/single-screen-pass/', import.meta.url))

async function capture(page: Page, name: string) {
  await page.screenshot({ path: `${finalScreenshotDir}/${name}`, fullPage: false })
}

async function submitCurrentChoice(page: Page) {
  await page.reload()
  await expect(page.getByTestId('team-gameplay-screen')).toBeVisible()
  const reaction = page.getByTestId('world-reaction')
  if (await reaction.count()) await reaction.getByRole('button', { name: /رد کردن واکنش‌ها|ادامه روایت/ }).last().click()
  await page.locator('.map-hotspot.state-new-information').first().click()
  await page.getByRole('button', { name: 'بررسی این نقطه' }).click()
  const choice = page.locator('.hc-choice-card').first()
  await expect(choice).toBeEnabled()
  await choice.click()
  await expect(page.getByText(/تصمیم حجره ثبت شد|واکنش بازار را دنبال کنید/).first()).toBeVisible()
}

async function expectNoDocumentOverflow(page: Page) {
  expect(await page.evaluate(() => ({
    horizontal: document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1,
    vertical: document.documentElement.scrollHeight <= window.innerHeight + 2,
  }))).toEqual({ horizontal: true, vertical: true })
}

async function expectNoHorizontalOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true)
}

async function expectGameplayInViewport(page: Page) {
  for (const selector of ['.hc-play-map-canvas', '.hc-narrative-block', '.hc-objective-card', '.hc-active-action']) {
    const box = await page.locator(selector).boundingBox()
    expect(box, `${selector} should have a box`).not.toBeNull()
    expect(box!.y).toBeGreaterThanOrEqual(0)
    expect(box!.y + box!.height).toBeLessThanOrEqual((await page.viewportSize())!.height + 1)
  }
}

test('real two-Team Hezar Cheragh playable slice preserves privacy through World Ending', async ({ browser, page: admin }) => {
  mkdirSync(finalScreenshotDir, { recursive: true })
  await admin.goto('/admin')
  await expect(admin.getByRole('heading', { name: 'راه‌اندازی جلسه هزارچراغ' })).toBeVisible()
  await admin.getByRole('button', { name: 'ساخت و پیکربندی جلسه' }).click()
  await expect(admin.getByRole('heading', { name: 'برگه کدهای جفت‌شدن' })).toBeVisible({ timeout: 30_000 })
  const sessionId = new URL(admin.url()).pathname.split('/')[3]
  expect(sessionId).toBeTruthy()
  const slips = admin.locator('.pairing-slips article')
  await expect(slips).toHaveCount(2)
  const codeA = (await slips.nth(0).locator('code').textContent())?.trim() ?? ''
  const codeB = (await slips.nth(1).locator('code').textContent())?.trim() ?? ''

  await admin.getByRole('button', { name: 'شروع جلسه' }).click()
  await expect(admin.getByRole('button', { name: 'مقداردهی روایت' })).toBeEnabled()
  await admin.getByRole('button', { name: 'مقداردهی روایت' }).click()
  await admin.getByRole('link', { name: 'اجرای زنده' }).click()

  const contextA = await browser.newContext({ locale: 'fa-IR' })
  const contextB = await browser.newContext({ locale: 'fa-IR' })
  const contextDisplay = await browser.newContext({ viewport: { width: 1600, height: 1000 }, locale: 'fa-IR' })
  const teamA = await contextA.newPage(); const teamB = await contextB.newPage(); const display = await contextDisplay.newPage()
  await teamA.goto('/pair'); await teamA.getByLabel('کد جفت‌شدن').fill(codeA); await teamA.getByRole('button', { name: 'ورود به بازار' }).click()
  await teamB.goto('/pair'); await teamB.getByLabel('کد جفت‌شدن').fill(codeB); await teamB.getByRole('button', { name: 'ورود به بازار' }).click()
  await expect(teamA).toHaveURL(/\/team\/story/); await expect(teamB).toHaveURL(/\/team\/story/)
  await display.goto(`/display/${sessionId}`)
  await expect(display.getByText('امروز زنگ بازار به صدا درنیامد.')).toBeVisible()
  await expect(teamA.getByTestId('team-gameplay-screen')).toBeVisible()
  await expect(display.locator('.market-scene-frame--display')).toBeVisible()
  await expect(teamA.locator('.visual-high canvas')).toBeVisible()
  await expect(display.locator('.visual-high canvas')).toBeVisible()
  await expect(teamA.locator('.map-hotspot')).toHaveCount(13)
  await expect(teamA.getByText('صبح', { exact: false }).first()).toBeVisible()
  await expect(teamA.getByText(/اعتماد بازار برقرار است|فشار بازار رو به افزایش است|انسجام بازار شکننده است|بازار در وضعیت بحرانی است/).first()).toBeVisible()
  await expect(teamA.getByText(/Pressure\s*[:=]?\s*\d+/i)).toHaveCount(0)
  await expect(display.getByText(/Pressure\s*[:=]?\s*\d+/i)).toHaveCount(0)
  await teamA.setViewportSize({ width: 1366, height: 768 })
  await expectNoDocumentOverflow(teamA)
  await expectGameplayInViewport(teamA)
  await capture(teamA, '01-main-1366x768.png')
  await teamA.setViewportSize({ width: 1440, height: 900 })
  await expectNoDocumentOverflow(teamA)
  await expectGameplayInViewport(teamA)
  await capture(teamA, '02-main-1440x900.png')
  await teamA.setViewportSize({ width: 1920, height: 1080 })
  await expectNoDocumentOverflow(teamA)
  await expectGameplayInViewport(teamA)
  await teamA.setViewportSize({ width: 1440, height: 900 })
  await expectNoHorizontalOverflow(display)

  await teamA.locator('.map-hotspot.state-new-information').first().click()
  await expect(teamA.locator('.hc-location-context')).toBeVisible()
  await capture(teamA, '03-investigation.png')
  await teamA.getByRole('button', { name: 'ورود به فعالیت کسب‌وکار' }).click()
  await expect(teamA.getByTestId('business-activity')).toBeVisible()
  await capture(teamA, '04-business-activity.png')
  await teamA.getByRole('button', { name: 'بازگشت به تصمیم پرده' }).click()
  await teamA.setViewportSize({ width: 820, height: 1180 })
  await expectNoDocumentOverflow(teamA)
  await capture(teamA, '10-tablet.png')
  await teamA.emulateMedia({ reducedMotion: 'reduce' })
  await teamA.reload()
  await expect(teamA.locator('.visual-reduced .fallback-market')).toBeVisible()
  await teamA.evaluate(() => {
    const raw = sessionStorage.getItem('hambaft-ui')
    const parsed = raw ? JSON.parse(raw) : { state: {}, version: 0 }
    parsed.state.visualMode = 'fallback'
    sessionStorage.setItem('hambaft-ui', JSON.stringify(parsed))
  })
  await teamA.reload()
  await expect(teamA.locator('.visual-fallback .fallback-market')).toBeVisible()
  await expectNoDocumentOverflow(teamA)
  await capture(teamA, '11-fallback.png')
  await teamA.emulateMedia({ reducedMotion: 'no-preference' })
  await teamA.evaluate(() => {
    const raw = sessionStorage.getItem('hambaft-ui')
    const parsed = raw ? JSON.parse(raw) : { state: {}, version: 0 }
    parsed.state.visualMode = 'high'
    sessionStorage.setItem('hambaft-ui', JSON.stringify(parsed))
  })
  await teamA.setViewportSize({ width: 1440, height: 900 })
  await teamA.reload()

  const privateA = await teamA.locator('.hc-narrative-copy').innerText()
  const privateB = await teamB.locator('.hc-narrative-copy').innerText()
  expect(privateA).not.toEqual(privateB)
  await expect(teamB.getByText(privateA, { exact: true })).toHaveCount(0)
  await expect(display.getByText(privateA, { exact: true })).toHaveCount(0)

  await submitCurrentChoice(teamA)
  await submitCurrentChoice(teamB)

  await teamA.reload()
  await teamA.getByRole('button', { name: /پیمان‌ها/ }).click()
  await teamA.getByRole('button', { name: 'انتخاب مقصد روی نقشه' }).click()
  await expect(teamA.locator('.map-hotspot.state-pact-target').first()).toBeVisible()
  await capture(teamA, '05-pact-target.png')
  await teamA.locator('.map-hotspot.state-pact-target').first().click()
  await expect(teamA.locator('.hc-proposal-letter')).toBeVisible()
  await capture(teamA, '06-proposal-letter.png')
  await teamA.getByLabel('مقدار').fill('8')
  await teamA.getByRole('button', { name: 'مهر و ارسال نامه' }).click()
  await expect(teamA.getByText(/پیام پیمان از مسیر بازار فرستاده شد/)).toBeVisible()

  await teamB.goto('/team/messages')
  await expect(teamB.getByRole('button', { name: 'پیشنهاد متقابل' })).toBeVisible()
  await teamB.getByRole('button', { name: 'پیشنهاد متقابل' }).click()
  await teamB.getByLabel('مقدار').fill('12')
  await capture(teamB, '07-counterproposal.png')
  await teamB.getByRole('button', { name: 'ارسال بازنگری تازه' }).click()

  await teamA.goto('/team/messages')
  teamA.once('dialog', (dialog) => dialog.accept())
  await teamA.getByRole('button', { name: 'پذیرش' }).click()
  await expect(teamA.getByText('پذیرفته‌شده').first()).toBeVisible()
  await teamA.goto('/team/story')
  await expect(teamA.getByTestId('team-gameplay-screen')).toBeVisible()
  await capture(teamA, '08-active-agreement.png')

  for (let checkpoint = 0; checkpoint < 4; checkpoint += 1) {
    if (checkpoint > 0) {
      await teamA.goto('/team/story'); await teamB.goto('/team/story')
      await submitCurrentChoice(teamA)
      await submitCurrentChoice(teamB)
    }
    await admin.reload()
    const resolve = admin.getByRole('button', { name: 'حل ایستگاه روایت' })
    await expect(resolve).toBeEnabled({ timeout: 20_000 })
    const oldVersion = await admin.locator('.runtime-stats article').last().locator('strong').textContent()
    await resolve.click()
    await expect(admin.locator('.runtime-stats article').last().locator('strong')).not.toHaveText(oldVersion ?? '', { timeout: 20_000 })
    await teamA.goto('/team/story')
    await expect(teamA.getByTestId('world-reaction')).toBeVisible({ timeout: 20_000 })
    if (checkpoint === 0) await capture(teamA, '09-world-reaction.png')
    while (await teamA.getByTestId('world-reaction').count()) {
      await teamA.getByTestId('world-reaction').getByRole('button', { name: /واکنش بعدی|ادامه روایت/ }).click()
    }
  }

  await admin.reload()
  const endings = admin.getByRole('button', { name: 'حل سرانجام‌ها' })
  await expect(endings).toBeEnabled({ timeout: 20_000 })
  await endings.click()

  await teamA.goto('/team/ending'); await teamB.goto('/team/ending')
  await expect(teamA.getByText('سرانجام حجره')).toBeVisible({ timeout: 20_000 })
  await expect(teamB.getByText('سرانجام حجره')).toBeVisible()
  await expect(teamA.getByText('سرانجام بازار')).toBeVisible()
  await display.reload()
  await expect(display.getByText('سرانجام بازار')).toBeVisible({ timeout: 20_000 })
  await expect(display.getByText(privateA, { exact: true })).toHaveCount(0)
  await capture(display, '13-world-ending.png')

  await teamA.reload()
  await expect(teamA.getByText('سرانجام حجره')).toBeVisible()
  await capture(teamA, '12-entity-ending.png')
  await expect(teamA.locator('.market-scene-frame')).toHaveAttribute('data-scene-id', /market-night|slice-complete/)
  let experienceReads = 0
  teamA.on('request', (request) => {
    if (request.method() === 'GET' && request.url().includes('/api/story/experience')) experienceReads += 1
  })
  const readsBeforeReconnect = experienceReads
  await contextA.setOffline(true)
  await expect(teamA.getByText(/ارتباط قطع است|در حال اتصال مجدد/)).toBeVisible({ timeout: 20_000 })
  await contextA.setOffline(false)
  await expect(teamA.getByText('متصل')).toBeVisible({ timeout: 30_000 })
  await expect.poll(() => experienceReads, { timeout: 20_000 }).toBeGreaterThan(readsBeforeReconnect)

  await expectNoHorizontalOverflow(admin)
  await expectNoHorizontalOverflow(display)

  await contextA.close(); await contextB.close(); await contextDisplay.close()
})
