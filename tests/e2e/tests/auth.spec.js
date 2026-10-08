import { test, expect, newOrganiser } from '../fixtures.js';

test.describe('Sign in', () => {
  test('rejects an empty email with a field-level message', async ({ page, app }) => {
    await app.signIn({ email: '', displayName: '' });

    await expect(page).toHaveURL(/\/Account\/SignIn/);
    await expect(page.getByText('Enter the email address you use for EKProno.')).toBeVisible();
  });

  test('rejects an address that is not an email', async ({ page, app }) => {
    await app.signIn({ email: 'not-an-email', displayName: '' });

    await expect(page).toHaveURL(/\/Account\/SignIn/);
    await expect(page.getByText('Enter the email address you use for EKProno.')).toBeVisible();
    // The bad input stays put, so it can be corrected rather than retyped.
    await expect(page.locator('#Email')).toHaveValue('not-an-email');
  });

  test('signs a new organiser in and shows them the empty state', async ({ page, app }) => {
    const organiser = newOrganiser();
    await app.signIn(organiser);

    await expect(page).toHaveURL(/\/Pools$/);
    await expect(page.getByRole('heading', { name: 'My pools' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'You have no pools yet' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Create your first pool' })).toBeVisible();
    await expect(page.getByText(organiser.displayName)).toBeVisible();
  });

  test('signing in again with the same email keeps the display name', async ({ page, app }) => {
    const organiser = newOrganiser();
    await app.signIn(organiser);
    await app.signOut();

    // No display name this time: the account is matched on email, not recreated.
    await app.signIn({ email: organiser.email });

    await expect(page).toHaveURL(/\/Pools$/);
    await expect(page.getByText(organiser.displayName)).toBeVisible();
  });
});

test.describe('Sign out', () => {
  test('returns to the home page and locks the pools again', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await app.signOut();
    await expect(page).toHaveURL(/127\.0\.0\.1:\d+\/$/);
    await expect(page.getByRole('link', { name: 'Sign in' })).toBeVisible();

    await page.goto('/Pools');
    await expect(page).toHaveURL(/\/Account\/SignIn/);
  });
});

test.describe('Protected routes', () => {
  test('an anonymous visitor is sent to sign in and then on to where they were going',
    async ({ page, app }) => {
      const organiser = newOrganiser();

      // Create a pool, sign out, then come back to it cold.
      await app.signIn(organiser);
      const poolId = await app.createPool({ name: 'Return url pool' });
      await app.signOut();

      await page.goto(`/Pools/Schedule/${poolId}`);
      await expect(page).toHaveURL(
        new RegExp(`/Account/SignIn\\?ReturnUrl=%2FPools%2FSchedule%2F${poolId}`, 'i'));

      await page.locator('#Email').fill(organiser.email);
      await page.getByRole('button', { name: 'Sign in' }).click();

      // Back to the page that triggered the challenge, not the generic landing page.
      await expect(page).toHaveURL(new RegExp(`/Pools/Schedule/${poolId}$`, 'i'));
      await expect(page.getByRole('heading', { name: /Europees Kampioenschap/ })).toBeVisible();
    });

  test('every page under /Pools requires an account', async ({ page }) => {
    for (const route of ['/Pools', '/Pools/Create']) {
      await page.goto(route);
      await expect(page).toHaveURL(/\/Account\/SignIn/);
    }
  });
});
