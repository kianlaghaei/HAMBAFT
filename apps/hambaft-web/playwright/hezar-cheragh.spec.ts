import { expect, test, type Page } from '@playwright/test'

async function submitCurrentChoice(page: Page) {
  await page.reload()
  await expect(page.locator('.story-sheet')).toBeVisible()
  const choice = page.locator('.choices button').first()
  await expect(choice).toBeEnabled()
  await choice.click()
  await expect(page.getByText(/تصمیم شما ثبت شد/)).toBeVisible()
}

async function expectNoHorizontalOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true)
}

test('real two-Team Hezar Cheragh playable slice preserves privacy through World Ending', async ({ browser, page: admin }) => {
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
  await expect(teamA.locator('.market-scene-frame')).toBeVisible()
  await expect(display.locator('.market-scene-frame--display')).toBeVisible()
  await expect(teamA.locator('.visual-high canvas')).toBeVisible()
  await expect(display.locator('.visual-high canvas')).toBeVisible()
  await expect(display.getByRole('list', { name: 'حجره‌های حاضر در نقشه' }).getByRole('listitem')).toHaveCount(4)
  await expectNoHorizontalOverflow(teamA)
  await expectNoHorizontalOverflow(display)

  const privateA = await teamA.locator('.narrative-copy').innerText()
  const privateB = await teamB.locator('.narrative-copy').innerText()
  expect(privateA).not.toEqual(privateB)
  await expect(teamB.getByText(privateA, { exact: true })).toHaveCount(0)
  await expect(display.getByText(privateA, { exact: true })).toHaveCount(0)

  await submitCurrentChoice(teamA)
  await submitCurrentChoice(teamB)

  await teamA.reload()
  await teamA.getByRole('link', { name: 'پیام‌ها' }).click()
  await expect(teamA.getByRole('heading', { name: 'نامه تازه', exact: true })).toBeVisible()
  await teamA.getByLabel('نوع تعامل').selectOption('emergency-supply')
  await teamA.getByLabel('مقدار').fill('8')
  await teamA.getByRole('button', { name: 'ارسال پیشنهاد' }).click()
  await expect(teamA.getByRole('heading', { name: 'تأمین اضطراری' }).last()).toBeVisible()

  await teamB.getByRole('link', { name: 'پیام‌ها' }).click()
  await expect(teamB.getByRole('button', { name: 'پیشنهاد متقابل' })).toBeVisible()
  await teamB.getByRole('button', { name: 'پیشنهاد متقابل' }).click()
  await teamB.getByLabel('مقدار').fill('12')
  await teamB.getByRole('button', { name: 'ارسال بازنگری تازه' }).click()

  await teamA.reload()
  teamA.once('dialog', (dialog) => dialog.accept())
  await teamA.getByRole('button', { name: 'پذیرش' }).click()
  await expect(teamA.getByText('پذیرفته‌شده').first()).toBeVisible()

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

  await teamA.reload()
  await expect(teamA.getByText('سرانجام حجره')).toBeVisible()
  await contextA.setOffline(true)
  await expect(teamA.getByText(/ارتباط قطع است|در حال اتصال مجدد/)).toBeVisible({ timeout: 20_000 })
  await contextA.setOffline(false)
  await expect(teamA.getByText('متصل')).toBeVisible({ timeout: 30_000 })

  await teamA.setViewportSize({ width: 820, height: 1180 })
  await teamA.goto('/team/ending')
  await expect(teamA.locator('.market-scene-frame')).toBeVisible()
  await expectNoHorizontalOverflow(teamA)
  await teamA.emulateMedia({ reducedMotion: 'reduce' })
  await teamA.reload()
  await expect(teamA.locator('.visual-reduced .fallback-market')).toBeVisible()
  await teamA.locator('.scene-controls select').selectOption('fallback')
  await expect(teamA.locator('.visual-fallback .fallback-market')).toBeVisible()
  await expectNoHorizontalOverflow(teamA)
  await expectNoHorizontalOverflow(admin)
  await expectNoHorizontalOverflow(display)

  await contextA.close(); await contextB.close(); await contextDisplay.close()
})
