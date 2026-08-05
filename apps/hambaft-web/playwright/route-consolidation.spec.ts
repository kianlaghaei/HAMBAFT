import { expect, test } from '@playwright/test'

test('Team legacy URLs never expose separate pages when unpaired', async ({ page }) => {
  for (const route of ['/team', '/team/story', '/team/market', '/team/messages', '/team/agreements', '/team/status', '/team/ending']) {
    await page.goto(route)
    await expect(page).toHaveURL(/\/pair$/)
    await expect(page.getByLabel('کد جفت‌شدن')).toBeVisible()
  }
})

test('Team entry and Admin entry remain separate for an unpaired browser', async ({ page }) => {
  await page.goto('/admin')
  await expect(page.getByRole('heading', { name: 'راه‌اندازی جلسه هزارچراغ' })).toBeVisible()
  await page.goto('/team')
  await expect(page).toHaveURL(/\/pair$/)
})

test('real pairing, refresh, legacy redirects, Back and Forward stay on canonical Team page', async ({ browser, page: admin }) => {
  await admin.goto('/admin')
  await expect(admin.getByRole('heading', { name: 'راه‌اندازی جلسه هزارچراغ' })).toBeVisible()
  await admin.getByRole('button', { name: 'ساخت و پیکربندی جلسه' }).click()
  await expect(admin.getByRole('heading', { name: 'برگه کدهای جفت‌شدن' })).toBeVisible({ timeout: 30_000 })
  const sessionId = new URL(admin.url()).pathname.split('/')[3]
  const code = (await admin.locator('.pairing-slips article').first().locator('code').textContent())?.trim() ?? ''
  await admin.getByRole('button', { name: 'شروع جلسه' }).click()
  await admin.getByRole('button', { name: 'مقداردهی روایت' }).click()
  await admin.getByRole('link', { name: 'اجرای زنده' }).click()

  const context = await browser.newContext({ locale: 'fa-IR' })
  const team = await context.newPage()
  await team.goto('/pair')
  await team.getByLabel('کد جفت‌شدن').fill(code)
  await team.getByRole('button', { name: 'ورود به بازار' }).click()
  await expect(team).toHaveURL(/\/team$/)
  await expect(team.getByTestId('team-gameplay-screen')).toBeVisible({ timeout: 30_000 })

  await team.reload()
  await expect(team).toHaveURL(/\/team$/)
  await expect(team.getByTestId('team-gameplay-screen')).toBeVisible({ timeout: 30_000 })
  for (const route of ['/team/story', '/team/market', '/team/messages', '/team/agreements', '/team/status', '/team/ending']) {
    await team.goto(route)
    await expect(team).toHaveURL(/\/team$/)
    await expect(team.getByTestId('team-gameplay-screen')).toBeVisible({ timeout: 30_000 })
  }
  await team.goto('/team')
  await team.goto('/team/messages')
  await expect(team).toHaveURL(/\/team$/)
  await team.goBack()
  await expect(team).toHaveURL(/\/team$/)
  await team.goForward()
  await expect(team).toHaveURL(/\/team$/)

  await expect(admin.getByRole('link', { name: 'نمایش عمومی' })).toBeVisible()
  await admin.goto(`/display/${sessionId}`)
  await expect(admin.locator('.market-scene-frame--display')).toBeVisible({ timeout: 30_000 })
  await context.close()
})
