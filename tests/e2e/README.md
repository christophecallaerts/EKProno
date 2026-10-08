# End-to-end tests

Playwright drives a real browser against a real `dotnet run`, so these cover what the unit
and integration tests cannot: the rendered pages, the forms, and the redirects between
them. They are a third suite alongside [tests/EKProno.Tests/](../EKProno.Tests/) — they do
not replace it, and anything that can be asserted without a browser belongs there instead.

## Running them

```bash
cd tests/e2e
npm install
npx playwright install --with-deps chromium   # once per machine
npm test                                      # headless
npm run test:headed                           # watch it happen
npm run test:ui                               # pick and re-run individual tests
npm run report                                # open the last HTML report
```

Playwright starts the app itself — there is no need to have one running. It builds and
launches the web project on **port 5299**, which is deliberately not the 5298 of
`dotnet run`, so a dev server you already have open keeps its data and its port.

## How a run stays isolated

Three things keep the suite deterministic and safe to run next to your own data:

- **Its own database.** `Program.cs` reads `DataStore:Path`; the config here points it at
  `.artifacts/ekprono.e2e.json`, and `global-setup.js` deletes that file before the web
  server starts. `App_Data/ekprono.json` is never touched.
- **Its own build output.** `--artifacts-path` sends `bin`/`obj` into `.artifacts/build`.
  Without it a `dotnet run` or `dotnet watch` you have open holds a lock on `bin/` and the
  E2E build fails with MSB3027.
- **Its own organiser per test.** Every test signs in as a freshly generated email address
  and owns only what it creates, which is what lets the files run in parallel.

Kickoffs render in the viewer's time zone (FR-009), so the config pins `Europe/Amsterdam`
and the fixtures seed the `ekprono-tz` cookie up front — otherwise `_TimeZonePartial`
reloads the first page it sees and every assertion has to tolerate that.

## Environment variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `E2E_PORT` | `5299` | Port the test server listens on. |
| `E2E_BASE_URL` | `http://127.0.0.1:$E2E_PORT` | Point at an app you started yourself. |
| `E2E_CONFIGURATION` | `Debug` | Build configuration for the test server. |
| `E2E_NO_BUILD` | unset | `true` to skip the build — CI sets this after its own. |

## Writing a test

Import from [`fixtures.js`](fixtures.js) rather than `@playwright/test` directly. It
provides an `app` page object, an `organiser` fixture that is already signed in, and the
`kickoffIn()` helper for dates.

```js
import { test, expect, kickoffIn } from '../fixtures.js';

test('adds a fixture', async ({ app, organiser }) => {
  const poolId = await app.createPool({ name: 'My Pool' });
  await app.addTeams(poolId, ['België', 'Nederland']);
  await app.gotoSchedule(poolId);

  await app.addFixture({ home: 'België', away: 'Nederland', kickoff: kickoffIn(30) });

  await expect(app.fixture('België', 'Nederland')).toBeVisible();
});
```

Submit through `app.submit(button)` instead of clicking it directly. These are plain
server-rendered form posts, and a `page.goto()` straight after a bare click races the
in-flight POST — which is how the suite first managed to arrive somewhere signed out.

## A deliberately failing test

`pools.spec.js` marks *"tells the organiser when the new name is empty"* with
`test.fail()`: renaming a pool to an empty name is refused in complete silence. The cause
is a field-name mismatch — `PoolService.ValidatePoolName` reports against `PoolName` while
the rename page binds and renders `Name`, so the message lands on a ModelState key nothing
displays. When that is fixed the test starts passing, Playwright reports the unexpected
pass as a failure, and the annotation should be removed.
