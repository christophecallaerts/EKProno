import { test, expect, newOrganiser } from '../fixtures.js';

/**
 * NFR-003 / SC-004: a pool that is not yours simply is not there. These assert on the
 * status code rather than page content, because "404 and not 403" is the whole point —
 * a 403 would confirm the pool exists to someone who should not know that.
 */
test.describe('One organiser cannot reach another organiser\'s pool', () => {
  test('every pool-scoped route answers 404', async ({ page, app }) => {
    const alice = newOrganiser('alice');
    await app.signIn(alice);
    const poolId = await app.createPool({ name: 'Alice Only Pool' });
    await app.addTeams(poolId, ['België', 'Nederland']);
    await app.signOut();

    const bob = newOrganiser('bob');
    await app.signIn(bob);

    for (const route of [
      `/Pools/Schedule/${poolId}`,
      `/Pools/Teams/${poolId}`,
      `/Pools/Rename/${poolId}`,
    ]) {
      const response = await page.goto(route);
      expect(response.status(), `${route} should 404 for a non-owner`).toBe(404);
    }
  });

  test('and it stays off their pool list', async ({ page, app }) => {
    const alice = newOrganiser('alice');
    await app.signIn(alice);
    await app.createPool({ name: 'Invisible To Bob' });
    await app.signOut();

    const bob = newOrganiser('bob');
    await app.signIn(bob);

    await expect(page.getByRole('heading', { name: 'You have no pools yet' })).toBeVisible();
    await expect(page.getByText('Invisible To Bob')).toHaveCount(0);
  });

  test('signing in through a ReturnUrl does not hand over the pool', async ({ page, app }) => {
    const alice = newOrganiser('alice');
    await app.signIn(alice);
    const poolId = await app.createPool({ name: 'Return Url Guarded Pool' });
    await app.signOut();

    // Bob follows a link to Alice's pool, gets challenged, and signs in as himself.
    await page.goto(`/Pools/Schedule/${poolId}`);
    await expect(page).toHaveURL(/\/Account\/SignIn/);

    const bob = newOrganiser('bob');
    await page.locator('#Email').fill(bob.email);
    await page.locator('#DisplayName').fill(bob.displayName);

    const [response] = await Promise.all([
      page.waitForResponse((r) => r.url().includes(`/Pools/Schedule/${poolId}`)),
      page.getByRole('button', { name: 'Sign in' }).click(),
    ]);

    // The ReturnUrl is honoured, but ownership is still checked at the far end.
    expect(response.status()).toBe(404);
  });
});

test.describe('Bad pool ids', () => {
  test('a malformed id is a 404, not a server error', async ({ page, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const response = await page.goto('/Pools/Schedule/not-a-guid');
    expect(response.status()).toBe(404);
  });

  test('a well-formed id that belongs to nobody is a 404', async ({ page, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const response = await page.goto('/Pools/Schedule/00000000-0000-0000-0000-000000000000');
    expect(response.status()).toBe(404);
  });
});
