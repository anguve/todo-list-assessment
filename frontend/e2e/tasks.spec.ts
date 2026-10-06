import { expect, test } from '@playwright/test';

/**
 * Registers, adds a task, edits it, deletes it, signs out, and signs in again.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
test('register, sign in, add a task, edit it, delete it, and sign out', async ({ page }) => {
  const email = `user.${Date.now()}@example.com`;
  const password = 'password1';

  await page.goto('/register');
  await page.getByLabel('Name').fill('Ada');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByLabel('Confirm password').fill(password);
  await page.getByRole('button', { name: 'Create account' }).click();

  await expect(page).toHaveURL(/\/todos$/);
  await expect(page.getByRole('heading', { name: 'Your tasks' })).toBeVisible();

  await page.getByLabel('New task').fill('Buy coffee');
  await page.getByLabel('Description').fill('From the corner shop');
  await page.getByRole('button', { name: 'Add' }).click();
  await expect(page.getByText('Buy coffee')).toBeVisible();
  await expect(page.getByText('From the corner shop')).toBeVisible();

  await page.getByRole('button', { name: 'Edit' }).click();
  await page.getByLabel('Title').fill('Buy tea');
  await page.getByLabel('Task description').fill('Loose leaf');
  await page.getByRole('button', { name: 'Save' }).click();
  await expect(page.getByText('Buy tea')).toBeVisible();
  await expect(page.getByText('Loose leaf')).toBeVisible();
  await expect(page.getByText('Buy coffee')).toHaveCount(0);

  await page.getByRole('button', { name: 'Delete' }).click();
  await expect(page.getByText('Buy tea')).toHaveCount(0);
  await expect(page.getByText('Nothing written down yet.')).toBeVisible();

  const token = await page.evaluate(() => localStorage.getItem('fieldbook.token'));
  expect(token).toBeTruthy();

  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect.poll(() => page.evaluate(() => localStorage.getItem('fieldbook.token'))).toBeNull();

  const rejected = await page.request.get('/api/todos', {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(rejected.status()).toBe(401);

  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/todos$/);
  await expect(page.getByText(email)).toBeVisible();
});
