import { expect, type Page } from '@playwright/test'
import { waitForApiHealthy } from './api'

/** Login via Continue and wait until the shell has hydrated past the login page. */
export async function continueToDashboard(page: Page): Promise<void> {
  await waitForApiHealthy()
  await page.goto('/#/')
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible()
  await page.getByRole('button', { name: 'Continue' }).click()
  await expect(page.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible({ timeout: 30_000 })
  // Hydration overlay should clear (nav + search usable).
  await expect(page.getByRole('navigation', { name: 'Primary navigation' })).toBeVisible()
  await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
}

export async function openNav(page: Page, label: string): Promise<void> {
  await page.getByRole('navigation', { name: 'Primary navigation' }).getByRole('link', { name: label }).click()
}

export async function openAiPanel(page: Page): Promise<void> {
  const panel = page.locator('aside[aria-label="AI panel"]')
  if (await panel.isVisible().catch(() => false)) return
  await page.getByRole('button', { name: 'AI ▸' }).click()
  await expect(panel).toBeVisible()
}

export async function uniqueTitle(prefix: string): Promise<string> {
  return `${prefix} ${Date.now()}-${Math.floor(Math.random() * 1000)}`
}
