import { test, expect, kickoffIn } from '../fixtures.js';

test.describe('Managing teams', () => {
  test('adds a team and confirms it', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Teams Pool' });
    await app.gotoTeams(poolId);

    await expect(page.getByText('No teams yet. Add at least two before you can enter a match.'))
      .toBeVisible();

    await app.addTeam('België');

    await expect(app.notice).toHaveText('Added België.');
    await expect(app.teamRow('België')).toBeVisible();
  });

  test('refuses a duplicate regardless of casing', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Duplicate Teams Pool' });
    await app.addTeams(poolId, ['België']);

    await app.addTeam('belgië');

    await expect(page.getByText('That team is already in the tournament.')).toBeVisible();
    await expect(app.teamRow('België')).toHaveCount(1);
  });

  test('lists the teams alphabetically', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Sorted Teams Pool' });
    await app.addTeams(poolId, ['Nederland', 'België', 'Italië', 'Frankrijk']);

    const names = await page
      .locator('li.list-group-item input[name="Name"]')
      .evaluateAll((inputs) => inputs.map((input) => input.value));

    expect(names).toEqual(['België', 'Frankrijk', 'Italië', 'Nederland']);
  });

  test('renames a team', async ({ app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Rename Team Pool' });
    await app.addTeams(poolId, ['Italië']);

    const row = app.teamRow('Italië');
    await row.locator('input[name="Name"]').fill('Italia');
    await row.getByRole('button', { name: 'Rename' }).click();

    await expect(app.notice).toHaveText('Renamed to Italia.');
    await expect(app.teamRow('Italia')).toBeVisible();
    await expect(app.teamRow('Italië')).toHaveCount(0);
  });

  test('removes a team that is not playing anything', async ({ app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'Remove Team Pool' });
    await app.addTeams(poolId, ['Spanje']);

    await app.teamRow('Spanje').getByRole('button', { name: 'Remove' }).click();

    await expect(app.teamRow('Spanje')).toHaveCount(0);
  });

  test('will not remove a team that has fixtures, and says how many', async ({ app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'In Use Team Pool' });
    await app.addTeams(poolId, ['België', 'Nederland', 'Frankrijk']);

    await app.gotoSchedule(poolId);
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30) });
    await app.addFixture({ home: 'België', away: 'Frankrijk', kickoff: kickoffIn(31) });

    await app.gotoTeams(poolId);

    // FR-005: the Remove button is replaced by the count that explains its absence.
    const belgium = app.teamRow('België');
    await expect(belgium.getByRole('button', { name: 'Remove' })).toHaveCount(0);
    await expect(belgium.locator('.badge')).toHaveText('2 matches');

    // Singular is spelled correctly too.
    await expect(app.teamRow('Nederland').locator('.badge')).toHaveText('1 match');
  });
});
