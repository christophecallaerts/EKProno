import { test, expect, kickoffIn } from '../fixtures.js';

/** A pool with four teams, which is the starting point for most of these tests. */
async function poolWithTeams(app, name) {
  const poolId = await app.createPool({ name });
  await app.addTeams(poolId, ['België', 'Nederland', 'Frankrijk', 'Italië']);
  await app.gotoSchedule(poolId);
  return poolId;
}

test.describe('Before there are teams', () => {
  test('offers no fixture form until two teams exist', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    const poolId = await app.createPool({ name: 'No Teams Pool' });

    // EC-1: nothing to pick between, so no form at all.
    await expect(page.getByText('Add at least two teams before entering fixtures')).toBeVisible();
    await expect(page.locator('#HomeTeamId')).toHaveCount(0);

    await app.addTeams(poolId, ['België']);
    await app.gotoSchedule(poolId);
    await expect(page.locator('#HomeTeamId')).toHaveCount(0);

    await app.addTeams(poolId, ['Nederland']);
    await app.gotoSchedule(poolId);
    await expect(page.locator('#HomeTeamId')).toBeVisible();
  });
});

test.describe('Adding a fixture', () => {
  test('shows it under its stage with the kickoff time', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Add Fixture Pool');
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30, '18:00') });

    await expect(app.notice).toHaveText('Match added.');
    await expect(page.getByText(/1 fixture/)).toBeVisible();

    const fixture = app.fixture('België', 'Nederland');
    await expect(fixture).toBeVisible();
    await expect(fixture.locator('.fixture__kickoff')).toHaveText('18:00');
    await expect(app.stageSection('Group stage')).toContainText('België');
  });

  test('refuses a team playing itself', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Self Play Pool');
    await app.addFixture({ home: 'België', away: 'België', kickoff: kickoffIn(30) });

    await expect(page.getByText('A team cannot play itself.')).toBeVisible();
    await expect(page.locator('.fixture')).toHaveCount(0);
  });

  test('refuses the same pairing twice in one stage', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Duplicate Fixture Pool');
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30, '18:00') });
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(31, '21:00') });

    await expect(page.getByText('België versus Nederland already exists in the group stage.'))
      .toBeVisible();
    await expect(page.locator('.fixture')).toHaveCount(1);
  });

  test('allows the same pairing again in a later stage', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Rematch Pool');
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30) });
    await app.addFixture({
      home: 'België', away: 'Nederland', kickoff: kickoffIn(60), stage: 'Final',
    });

    await expect(page.locator('.fixture')).toHaveCount(2);
    await expect(app.stageSection('Final')).toContainText('Nederland');
  });

  test('warns, but still saves, when the kickoff is already past', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Past Kickoff Pool');
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(-30) });

    // EC-2: allowed, but never silently.
    await expect(app.warning).toHaveText(
      'That kickoff is in the past, so predictions for this match are already locked.');
    await expect(app.fixture('België', 'Nederland')).toBeVisible();
  });
});

test.describe('Schedule ordering', () => {
  test('groups by stage in tournament order, kickoff ascending inside each',
    async ({ page, app, organiser }) => {
      expect(organiser.email).toBeTruthy();

      await poolWithTeams(app, 'Ordering Pool');

      // Entered deliberately out of order.
      await app.addFixture({
        home: 'België', away: 'Italië', kickoff: kickoffIn(90), stage: 'Final',
      });
      await app.addFixture({
        home: 'Frankrijk', away: 'Italië', kickoff: kickoffIn(30, '21:00'),
      });
      await app.addFixture({
        home: 'België', away: 'Nederland', kickoff: kickoffIn(30, '18:00'),
      });
      await app.addFixture({
        home: 'Nederland', away: 'Italië', kickoff: kickoffIn(60), stage: 'Semi-final',
      });

      // FR-017/FR-018: stages come out in tournament order, not entry order.
      await expect(app.stageNames).toHaveText(['Group stage', 'Semi-final', 'Final']);

      // The two group matches sort by kickoff, so the 18:00 one comes first.
      const groupKickoffs = app.stageSection('Group stage').locator('.fixture__kickoff');
      await expect(groupKickoffs).toHaveText(['18:00', '21:00']);

      await expect(app.stageSection('Group stage').locator('.stage__count')).toHaveText('2');
    });
});

test.describe('Editing a fixture', () => {
  test('moves a match to another stage and time', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Edit Fixture Pool');
    await app.addFixture({ home: 'België', away: 'Italië', kickoff: kickoffIn(30, '18:00') });

    await app.openEdit('België', 'Italië');
    await expect(page.getByRole('heading', { name: 'Edit match' })).toBeVisible();

    // The form arrives filled in with what is already there.
    await expect(page.locator('#Stage')).toHaveValue('GroupStage');

    await page.locator('#Stage').selectOption({ label: 'Final' });
    await page.locator('#LocalKickoff').fill(kickoffIn(90, '20:00'));
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(app.notice).toHaveText('Match updated.');
    await expect(app.stageNames).toHaveText(['Final']);
    await expect(app.fixture('België', 'Italië').locator('.fixture__kickoff')).toHaveText('20:00');
  });

  test('refuses an edit that duplicates another fixture', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Edit Clash Pool');
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30, '18:00') });
    await app.addFixture({ home: 'Frankrijk', away: 'Italië', kickoff: kickoffIn(30, '21:00') });

    await app.openEdit('Frankrijk', 'Italië');
    await page.locator('#HomeTeamId').selectOption({ label: 'België' });
    await page.locator('#AwayTeamId').selectOption({ label: 'Nederland' });
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(page.getByText('België versus Nederland already exists in the group stage.'))
      .toBeVisible();
  });

  test('cancel leaves the fixture alone', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Cancel Edit Pool');
    await app.addFixture({ home: 'België', away: 'Italië', kickoff: kickoffIn(30, '18:00') });

    await app.openEdit('België', 'Italië');
    await page.locator('#Stage').selectOption({ label: 'Final' });
    await page.getByRole('link', { name: 'Cancel' }).click();

    await expect(app.stageNames).toHaveText(['Group stage']);
    await expect(app.fixture('België', 'Italië').locator('.fixture__kickoff')).toHaveText('18:00');
  });
});

test.describe('Deleting a fixture', () => {
  test('removes it and leaves the rest of the schedule alone', async ({ page, app, organiser }) => {
    expect(organiser.email).toBeTruthy();

    await poolWithTeams(app, 'Delete Fixture Pool');
    await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30, '18:00') });
    await app.addFixture({ home: 'Frankrijk', away: 'Italië', kickoff: kickoffIn(30, '21:00') });

    await app.fixture('België', 'Nederland').getByRole('button', { name: 'Delete' }).click();

    await expect(app.notice).toHaveText('Match deleted.');
    await expect(app.fixture('België', 'Nederland')).toHaveCount(0);
    await expect(app.fixture('Frankrijk', 'Italië')).toBeVisible();
    await expect(page.locator('.fixture')).toHaveCount(1);
  });

  test('deleting the last fixture of a stage drops the stage heading too',
    async ({ page, app, organiser }) => {
      expect(organiser.email).toBeTruthy();

      await poolWithTeams(app, 'Empty Stage Pool');
      await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30) });

      await app.fixture('België', 'Nederland').getByRole('button', { name: 'Delete' }).click();

      await expect(app.stageNames).toHaveCount(0);
      await expect(page.getByText('No fixtures yet.')).toBeVisible();
    });
});
