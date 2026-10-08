import { test, expect } from '../fixtures.js';

test.describe('Creating a pool', () => {
  test('requires a name', async ({ page, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await page.goto('/Pools/Create');
    await page.getByRole('button', { name: 'Create pool' }).click();

    await expect(page).toHaveURL(/\/Pools\/Create/);
    await expect(page.getByText('A pool name is required.')).toBeVisible();
  });

  test('takes the organiser straight to the new schedule', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({
      name: 'Vrienden EK Pool',
      tournamentName: 'Europees Kampioenschap',
      edition: '2028',
    });

    await expect(page).toHaveURL(new RegExp(`/Pools/Schedule/${poolId}$`, 'i'));
    await expect(page.getByRole('heading', { name: 'Europees Kampioenschap 2028' })).toBeVisible();
    await expect(page.getByText('Vrienden EK Pool')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Teams (0)' })).toBeVisible();
  });

  test('refuses a second pool with the same name', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await app.createPool({ name: 'Duplicate Pool' });
    await app.createPool({ name: 'Something else' });

    await page.goto('/Pools/Create');
    await page.locator('#useNew').check();
    await page.locator('#PoolName').fill('Duplicate Pool');
    await page.locator('#TournamentName').fill('Europees Kampioenschap');
    await page.locator('#TournamentEdition').fill('2032');
    await page.getByRole('button', { name: 'Create pool' }).click();

    await expect(page.getByText('You already have a pool with that name.')).toBeVisible();
  });

  test('refuses re-entering a tournament it already knows', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await app.createPool({
      name: 'First Pool', tournamentName: 'Wereldbeker', edition: '2030',
    });

    await page.goto('/Pools/Create');
    await page.locator('#useNew').check();
    await page.locator('#PoolName').fill('Second Pool');
    await page.locator('#TournamentName').fill('Wereldbeker');
    await page.locator('#TournamentEdition').fill('2030');
    await page.getByRole('button', { name: 'Create pool' }).click();

    await expect(
      page.getByText('You already entered that tournament — select it from the list instead.'),
    ).toBeVisible();
  });

  test('can hang a second pool off a tournament already entered', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await app.createPool({
      name: 'Office Pool', tournamentName: 'Wereldbeker', edition: '2034',
    });

    // FR-004: the second pool picks the tournament from the list instead of retyping it.
    await page.goto('/Pools/Create');
    await page.locator('#useExisting').check();
    await page.locator('#TournamentId').selectOption({ label: 'Wereldbeker 2034' });
    await page.locator('#PoolName').fill('Family Pool');
    await page.getByRole('button', { name: 'Create pool' }).click();

    await expect(page).toHaveURL(/\/Pools\/Schedule\//);
    await expect(page.getByRole('heading', { name: 'Wereldbeker 2034' })).toBeVisible();
    await expect(page.getByText('Family Pool')).toBeVisible();
  });
});

test.describe('My pools', () => {
  test('lists the pools with links to the schedule and rename', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Listed Pool' });
    await page.goto('/Pools');

    const row = page.locator('li.list-group-item').filter({ hasText: 'Listed Pool' });
    await expect(row).toBeVisible();
    await expect(row.getByRole('link', { name: 'Schedule' })).toHaveAttribute(
      'href', new RegExp(`/Pools/Schedule/${poolId}$`, 'i'));
    await expect(row.getByRole('link', { name: 'Rename' })).toHaveAttribute(
      'href', new RegExp(`/Pools/Rename/${poolId}$`, 'i'));
  });
});

test.describe('Renaming a pool', () => {
  test('saves the new name and returns to the list', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Before Rename' });
    await page.goto(`/Pools/Rename/${poolId}`);

    await expect(page.locator('#Name')).toHaveValue('Before Rename');
    await page.locator('#Name').fill('After Rename');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page).toHaveURL(/\/Pools$/);
    await expect(page.getByText('After Rename')).toBeVisible();
    await expect(page.getByText('Before Rename')).toHaveCount(0);
  });

  test('refuses a name another of my pools already uses', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await app.createPool({ name: 'Taken Name' });
    const poolId = await app.createPool({ name: 'Rename Me' });

    await page.goto(`/Pools/Rename/${poolId}`);
    await page.locator('#Name').fill('Taken Name');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText('You already have a pool with that name.')).toBeVisible();
  });

  // Known bug, kept as an expected failure so it is not forgotten and so that fixing it
  // turns this file red until the annotation is removed.
  //
  // PoolService.ValidatePoolName reports the error against the field "PoolName", but
  // RenameModel binds the name as `Name` and Rename.cshtml renders
  // asp-validation-for="Name". The message lands on a ModelState key nothing renders, and
  // the page's asp-validation-summary="ModelOnly" excludes property errors by design — so
  // an empty name is rejected in total silence. Create works only because its bound
  // property happens to be called PoolName.
  test.fail('tells the organiser when the new name is empty', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Keeps Its Name' });
    await page.goto(`/Pools/Rename/${poolId}`);

    await page.locator('#Name').fill('   ');
    await page.getByRole('button', { name: 'Save' }).click();

    // The rename is correctly refused — we are still on the form, not back at the list.
    await expect(page).toHaveURL(new RegExp(`/Pools/Rename/${poolId}$`, 'i'));
    // ...but nothing on screen says why.
    await expect(page.getByText('A pool name is required.')).toBeVisible();
  });
});
