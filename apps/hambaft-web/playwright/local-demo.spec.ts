import { expect, test } from '@playwright/test'

test('ASP.NET serves the built SPA and restores a nested route', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'HAMBAFT' })).toBeVisible()
  await page.goto('/pair')
  await expect(page.getByRole('heading', { name: 'درِ حجره را باز کنید' })).toBeVisible()
  expect(await page.evaluate(async () => (await fetch('/ready')).ok)).toBe(true)
})
