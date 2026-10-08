import { test as base, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

const POOL_ID_IN_URL = /\/Pools\/Schedule\/([0-9a-f-]{36})/i;

// An organiser may enter a given tournament only once, so each pool a test creates needs
// an edition of its own. Per-process is unique enough: organisers are unique per test and
// a test never runs across two workers.
let editionCounter = 0;
const nextEdition = () => String(2030 + editionCounter++);

/** A throwaway organiser. Unique per test, which is what keeps the specs independent. */
export function newOrganiser(label = 'organiser') {
  const id = randomUUID().slice(0, 8);
  return { email: `${label}-${id}@example.com`, displayName: `${label}-${id}` };
}

/**
 * A kickoff `days` from now, as the `YYYY-MM-DDTHH:mm` a datetime-local input wants.
 * Relative rather than hard-coded so the suite does not quietly rot into the past —
 * a kickoff that has been and gone renders as "Kicked off" and hides the Edit link.
 */
export function kickoffIn(days, time = '18:00') {
  const date = new Date();
  date.setUTCDate(date.getUTCDate() + days);
  return `${date.toISOString().slice(0, 10)}T${time}`;
}

export class App {
  constructor(page) {
    this.page = page;
  }

  /**
   * Clicks a submit button and waits for the page it lands on.
   *
   * Every form here is a plain server-rendered POST, and the landing page differs by
   * outcome — a redirect when it worked, the same form re-rendered when it did not. Web
   * first assertions cope with that on their own, but a bare `page.goto()` immediately
   * after a click does not: it races the in-flight POST and can arrive before the
   * authentication cookie is set. Waiting for the navigation once, here, removes that
   * whole class of flake instead of sprinkling waits through the specs.
   */
  async submit(clickable) {
    await Promise.all([
      this.page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
      clickable.click(),
    ]);
  }

  // ---------------------------------------------------------------- account

  async signIn({ email, displayName }) {
    await this.page.goto('/Account/SignIn');
    await this.page.locator('#Email').fill(email);
    if (displayName !== undefined) {
      await this.page.locator('#DisplayName').fill(displayName);
    }
    await this.submit(this.page.getByRole('button', { name: 'Sign in' }));
  }

  async signOut() {
    await this.submit(this.page.getByRole('button', { name: 'Sign out' }));
  }

  // ------------------------------------------------------------------ pools

  /**
   * Creates a pool against a brand new tournament and returns its id. The edition
   * defaults to a fresh one per call: repeating a name and edition is refused with
   * "You already entered that tournament", which is a rule of its own and not what a
   * test asking for "just a pool" means to exercise.
   */
  async createPool({ name, tournamentName = 'Europees Kampioenschap', edition = nextEdition() }) {
    await this.page.goto('/Pools/Create');

    // The radio pair only appears once this organiser owns a tournament; from the second
    // pool onwards we have to say explicitly that we want a new one.
    const useNew = this.page.locator('#useNew');
    if (await useNew.count()) {
      await useNew.check();
    }

    await this.page.locator('#PoolName').fill(name);
    await this.page.locator('#TournamentName').fill(tournamentName);
    await this.page.locator('#TournamentEdition').fill(edition);
    await this.submit(this.page.getByRole('button', { name: 'Create pool' }));

    // FR-015: creating a pool lands you straight on its schedule.
    await this.page.waitForURL(POOL_ID_IN_URL);
    return this.page.url().match(POOL_ID_IN_URL)[1];
  }

  // ------------------------------------------------------------------ teams

  async gotoTeams(poolId) {
    await this.page.goto(`/Pools/Teams/${poolId}`);
  }

  async addTeam(name) {
    await this.page.locator('#Name').fill(name);
    await this.submit(this.page.getByRole('button', { name: 'Add team' }));
  }

  async addTeams(poolId, names) {
    await this.gotoTeams(poolId);
    for (const name of names) {
      await this.addTeam(name);
    }
  }

  /** The row for one team, so Rename/Remove can be reached without index juggling. */
  teamRow(name) {
    return this.page.locator('li.list-group-item').filter({
      has: this.page.locator(`input[aria-label="Name of ${name}"]`),
    });
  }

  // --------------------------------------------------------------- schedule

  async gotoSchedule(poolId) {
    await this.page.goto(`/Pools/Schedule/${poolId}`);
  }

  async addFixture({ home, away, kickoff, stage = 'Group stage' }) {
    await this.page.locator('#HomeTeamId').selectOption({ label: home });
    await this.page.locator('#AwayTeamId').selectOption({ label: away });
    await this.page.locator('#LocalKickoff').fill(kickoff);
    await this.page.locator('#Stage').selectOption({ label: stage });
    await this.submit(this.page.getByRole('button', { name: 'Add', exact: true }));
  }

  /**
   * Opens a fixture's edit page and waits for it. The edit form reuses the field ids of
   * the add-a-fixture form on the schedule, so acting before the navigation commits would
   * quietly drive the wrong page.
   */
  async openEdit(home, away) {
    await this.submit(this.fixture(home, away).getByRole('link', { name: 'Edit' }));
  }

  fixture(home, away) {
    return this.page
      .locator('.fixture')
      .filter({ has: this.page.locator('.fixture__team--home', { hasText: home }) })
      .filter({ has: this.page.locator('.fixture__team--away', { hasText: away }) });
  }

  /** Stage headings in the order they are rendered — FR-017's tournament order. */
  get stageNames() {
    return this.page.locator('.stage__name');
  }

  stageSection(stageName) {
    return this.page.locator('.stage').filter({
      has: this.page.locator('.stage__name', { hasText: stageName }),
    });
  }

  get notice() {
    return this.page.locator('.alert-success');
  }

  get warning() {
    return this.page.locator('.alert-warning');
  }
}

export const test = base.extend({
  // Seed the time-zone cookie up front. _TimeZonePartial sets it from JavaScript and
  // reloads the page when it was missing; pre-seeding removes that reload from every
  // first navigation, and with it a race no assertion should have to tolerate.
  context: async ({ context, baseURL }, use) => {
    await context.addCookies([
      { name: 'ekprono-tz', value: 'Europe/Amsterdam', url: baseURL },
    ]);
    await use(context);
  },

  app: async ({ page }, use) => {
    await use(new App(page));
  },

  /** An organiser who is already signed in, for the tests that start past the door. */
  organiser: async ({ app }, use) => {
    const identity = newOrganiser();
    await app.signIn(identity);
    await use(identity);
  },
});

export { expect };
