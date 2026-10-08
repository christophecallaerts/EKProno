# Feature Specification: Create a Pool

<!--
  TEMPLATE INSTRUCTIONS
  =====================
  Fill in each section focusing on WHAT the feature does and WHY — not HOW it should
  be implemented.

  Usage:
  - One spec per feature or functional slice
  - Store in docs/specs/ and version-control alongside your code
  - Use [NEEDS CLARIFICATION: question] markers for unresolved decisions (max 3)
  - Remove optional sections that don't apply — don't leave them as N/A
  - Reference your arc42 architecture docs where relevant rather than duplicating them
-->

## 1. Overview

| Field           | Value                                                               |
| --------------- | ------------------------------------------------------------------- |
| Feature ID      | 001                                                                 |
| Status          | Draft                                                               |
| Author          | Christophe                                                          |
| Created         | 2026-10-08                                                          |
| Last updated    | 2026-10-08                                                          |
| Epic / Parent   | [Story map — Set up the pool](../product/story-map.md)              |
| Arc42 reference | 3. Context & Scope, 5. Building Block View, 8. Crosscutting Concepts |

### 1.1 Problem Statement

Nothing in EKProno can happen until a pool exists: there is no schedule to fill, no
scoring rules to apply, no join link to share and no leaderboard to climb. An organiser
needs a single, fast action that turns "me and my mates want to predict the EK" into a
real, owned container for the tournament they care about.

### 1.2 Goal

An authenticated organiser can create a named pool that follows exactly one tournament.
On creation the pool is immediately usable as a container: it has an owner, the organiser
as its first player, a default set of scoring rules, and a join token ready to be shared.

### 1.3 Non-Goals

- Account registration, login and password management — assumed to already exist
  (see §9.1). This spec only consumes an authenticated identity.
- Entering teams and fixtures — covered by [spec 002](002-enter-match-schedule.md).
- Choosing the point values — covered by [spec 003](003-scoring-rules.md). Pool creation
  only installs defaults.
- Inviting players and the join flow — stories #004 and #005.
- Co-organisers or transferring ownership. A pool has exactly one organiser.
- Deleting or archiving a pool.
- A public directory of pools. Pools are private and reachable only by their organiser
  or via the join link.

## 2. User Stories

### US-001: Create a pool for a tournament

**As an** organiser,
**I want** to create a pool with a name and pick the tournament it follows,
**so that** my friend group has one place where all predictions for that tournament live.

### US-002: Find my way back to my pools

**As an** organiser,
**I want** to see the pools I have created,
**so that** I can return to the right one to manage its schedule, rules and results.

### US-003: Fix the pool name

**As an** organiser,
**I want** to rename a pool after creating it,
**so that** a typo or a change of heart does not force me to start over.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-001 | The system shall allow an authenticated user to create a pool by supplying a pool name and a tournament.                                     | Must     | US-001     |
| FR-002 | The system shall require a pool name of 1–100 characters after trimming surrounding whitespace.                                              | Must     | US-001     |
| FR-003 | The system shall reject a pool name that the same organiser has already used for another pool, case-insensitively.                           | Must     | US-001     |
| FR-004 | The system shall let the organiser either select one of the tournaments they already own or create a new one by supplying a name and edition. | Must     | US-001     |
| FR-005 | The system shall require a tournament name of 1–100 characters and an edition of 1–20 characters, and shall reject a duplicate name+edition pair for the same organiser. | Must     | US-001     |
| FR-006 | The system shall record the creating user as the pool's organiser.                                                                           | Must     | US-001     |
| FR-007 | The system shall add the organiser to the pool as its first player, with a display name defaulted from their account and editable by them.    | Must     | US-001     |
| FR-008 | The system shall create exactly one set of scoring rules per pool at creation time, using the system defaults defined in [spec 003](003-scoring-rules.md). | Must     | US-001     |
| FR-009 | The system shall generate a unique, unguessable join token for the pool at creation time.                                                     | Must     | US-001     |
| FR-010 | The system shall reject pool creation by an unauthenticated visitor.                                                                          | Must     | US-001     |
| FR-011 | The system shall show the organiser a list of the pools they own, most recently created first, each showing its name and tournament.           | Must     | US-002     |
| FR-012 | The system shall show a first-time organiser, who owns no pools, a prompt to create one.                                                      | Should   | US-002     |
| FR-013 | The system shall allow the organiser to rename their pool at any time, subject to FR-002 and FR-003.                                           | Should   | US-003     |
| FR-014 | The system shall not allow the tournament a pool follows to be changed after creation.                                                        | Must     | US-001     |
| FR-015 | The system shall take the organiser to the new pool's schedule entry screen immediately after creation.                                       | Should   | US-001     |

## 4. Acceptance Scenarios

### SC-001: Create a pool with a new tournament (FR-001, FR-004, FR-006)

```gherkin
Given I am signed in as an organiser
  And I own no tournaments yet
When I create a pool named "Vrienden EK" following a new tournament "Europees Kampioenschap" edition "2028"
Then the pool "Vrienden EK" exists and follows "Europees Kampioenschap 2028"
  And I am recorded as its organiser
```

### SC-002: Create a second pool for an existing tournament (FR-004)

```gherkin
Given I am signed in as an organiser
  And I already own the tournament "Europees Kampioenschap 2028"
When I create a pool named "Collega's EK" and select that existing tournament
Then the pool "Collega's EK" exists and follows "Europees Kampioenschap 2028"
  And no duplicate tournament is created
```

### SC-003: A new pool is immediately usable (FR-007, FR-008, FR-009)

```gherkin
Given I am signed in as an organiser
When I create a pool named "Vrienden EK"
Then I am a player in that pool
  And the pool has one set of scoring rules holding the system defaults
  And the pool has a join token
```

### SC-004: Blank pool name is rejected (FR-002)

```gherkin
Given I am signed in as an organiser
When I try to create a pool whose name is empty or only whitespace
Then the pool is not created
  And I am told the pool name is required
```

### SC-005: Duplicate pool name for the same organiser is rejected (FR-003)

```gherkin
Given I am signed in as an organiser
  And I already own a pool named "Vrienden EK"
When I try to create another pool named "vrienden ek"
Then the pool is not created
  And I am told I already have a pool with that name
```

### SC-006: Two organisers may use the same pool name (FR-003)

```gherkin
Given an organiser named Ann owns a pool named "Vrienden EK"
  And I am signed in as a different organiser
When I create a pool named "Vrienden EK"
Then my pool is created
  And Ann's pool is unaffected
```

### SC-007: Anonymous visitor cannot create a pool (FR-010)

```gherkin
Given I am not signed in
When I try to create a pool
Then no pool is created
  And I am asked to sign in first
```

### SC-008: Organiser sees only their own pools (FR-011)

```gherkin
Given I am signed in as an organiser
  And I own the pools "Vrienden EK" and "Collega's EK"
  And another organiser owns a pool named "Familie EK"
When I open my pool list
Then I see "Collega's EK" and "Vrienden EK"
  And I do not see "Familie EK"
```

### SC-009: Rename a pool (FR-013)

```gherkin
Given I am signed in as the organiser of a pool named "Vrienden EK"
When I rename it to "Vrienden EK 2028"
Then the pool is named "Vrienden EK 2028"
  And its tournament, players, scoring rules and join token are unchanged
```

### SC-010: Tournament cannot be swapped after creation (FR-014)

```gherkin
Given I am signed in as the organiser of a pool following "Europees Kampioenschap 2028"
When I attempt to make the pool follow a different tournament
Then the pool still follows "Europees Kampioenschap 2028"
  And I am told the tournament of a pool cannot be changed
```

## 5. Domain Model

### 5.1 Entities

#### UserAccount

The login identity behind an organiser or player. Introduced by this spec; owned by the
authentication feature referenced in §9.1.

| Attribute   | Type     | Constraints                      | Description                                  |
| ----------- | -------- | -------------------------------- | -------------------------------------------- |
| id          | UUID     | PK, generated                    |                                              |
| email       | string   | required, unique, max 255 chars  | Login identifier.                             |
| displayName | string   | required, max 50 chars           | Default display name when joining a pool.     |
| createdAt   | datetime | generated, immutable             |                                              |

#### Pool

A prediction pool for one tournament, owned by an organiser and joined by players through
a shareable link.

| Attribute    | Type     | Constraints                                     | Description                                       |
| ------------ | -------- | ----------------------------------------------- | ------------------------------------------------- |
| id           | UUID     | PK, generated                                   |                                                   |
| name         | string   | required, 1–100 chars, unique per organiser (ci) | The pool's name as the friend group knows it.     |
| organiserId  | UUID     | required, FK → UserAccount, immutable           | The user who created the pool.                     |
| tournamentId | UUID     | required, FK → Tournament, immutable            | The tournament the pool follows.                   |
| joinToken    | string   | required, unique, ≥ 128 bits of entropy         | Secret used by the join link (story #004).         |
| createdAt    | datetime | generated, immutable                            |                                                   |

#### Tournament

The football competition a pool follows. Created by the organiser; its teams and fixtures
are entered in [spec 002](002-enter-match-schedule.md).

| Attribute   | Type     | Constraints                                           | Description                                 |
| ----------- | -------- | ----------------------------------------------------- | ------------------------------------------- |
| id          | UUID     | PK, generated                                         |                                             |
| name        | string   | required, 1–100 chars                                 | e.g. "Europees Kampioenschap".               |
| edition     | string   | required, 1–20 chars                                  | e.g. "2028".                                 |
| ownerId     | UUID     | required, FK → UserAccount, immutable                 | The organiser who entered it.                |
|             |          | unique on (ownerId, name, edition), case-insensitive  |                                             |
| createdAt   | datetime | generated, immutable                                  |                                             |

#### Player

A participant in a pool, identified by their display name within that pool.

| Attribute   | Type     | Constraints                               | Description                                        |
| ----------- | -------- | ----------------------------------------- | -------------------------------------------------- |
| id          | UUID     | PK, generated                             |                                                    |
| poolId      | UUID     | required, FK → Pool, immutable            |                                                    |
| userId      | UUID     | required, FK → UserAccount, immutable     | Unique together with poolId.                        |
| displayName | string   | required, 1–50 chars, unique within pool   | Defaults from UserAccount.displayName.              |
| isOrganiser | boolean  | required                                   | True for exactly one player per pool.               |
| joinedAt    | datetime | generated, immutable                       |                                                    |

#### ScoringRules

The pool's point configuration. Created with defaults here; edited in
[spec 003](003-scoring-rules.md), which owns its attributes.

### 5.2 Relationships

- A **UserAccount** owns many **Pools**; a **Pool** has exactly one organiser **UserAccount**.
- A **UserAccount** owns many **Tournaments**; a **Tournament** belongs to exactly one **UserAccount**.
- A **Pool** follows exactly one **Tournament**; a **Tournament** may be followed by many **Pools**.
- A **Pool** has exactly one **ScoringRules**; **ScoringRules** belongs to exactly one **Pool**.
- A **Pool** has many **Players**; a **Player** belongs to exactly one **Pool**.
- A **Player** is backed by exactly one **UserAccount**; a **UserAccount** may be a **Player**
  in many **Pools**, but at most once per pool.

### 5.3 Value Objects

#### JoinToken

| Attribute | Type   | Constraints                                                    |
| --------- | ------ | -------------------------------------------------------------- |
| value     | string | required, unique across all pools, ≥ 128 bits of entropy, URL-safe |

### 5.4 Domain Rules and Invariants

- **Single organiser**: every pool has exactly one player with `isOrganiser = true`, and
  that player's `userId` equals the pool's `organiserId`.
- **Organiser is a player**: the organiser is always a member of their own pool and counts
  in the leaderboard like anyone else.
- **Pool name uniqueness is per organiser**: two different organisers may each own a pool
  called "Vrienden EK"; one organiser may not.
- **Tournament is immutable on a pool**: once set at creation, a pool's tournament never
  changes. Changing it would invalidate every schedule, prediction and score beneath it.
- **Every pool has scoring rules**: a pool never exists without exactly one associated
  `ScoringRules`. Creating a pool and creating its rules succeed or fail together.
- **Join token is secret and stable**: generated once at creation, never derived from the
  pool name or id, and not rotated by this feature.
- **Display name is unique within a pool**: two players in the same pool cannot share a
  display name, so the leaderboard is never ambiguous.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                               |
| ------- | ------------ | --------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance  | Creating a pool completes in < 500 ms at p95, measured server-side.                                        |
| NFR-002 | Performance  | The organiser's pool list renders in < 300 ms at p95 for an organiser owning up to 20 pools.                |
| NFR-003 | Security     | Only an authenticated user may create a pool; only a pool's organiser may view it in the pool list or rename it. |
| NFR-004 | Security     | The join token is generated from a cryptographically secure random source and is never logged in full.      |
| NFR-005 | Reliability  | Pool creation is atomic: a pool, its scoring rules and its organiser player row are all created or none are. |
| NFR-006 | Usability    | Pool creation requires at most one screen and no more than three inputs (pool name, tournament name, edition). |

## 7. Edge Cases and Error Scenarios

| ID   | Scenario                                                                 | Expected Behavior                                                                               |
| ---- | ------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| EC-1 | Pool name is empty, whitespace only, or longer than 100 characters        | Reject with a field-level validation message; nothing is persisted.                              |
| EC-2 | Pool name duplicates one of the organiser's existing pools                | Reject with "you already have a pool with that name"; the existing pool is untouched.            |
| EC-3 | Organiser submits the creation form twice (double click, retry)           | Exactly one pool is created; the second submission is rejected by EC-2 or ignored as idempotent. |
| EC-4 | Organiser selects a tournament owned by somebody else                     | Reject as not found; do not disclose that the tournament exists.                                 |
| EC-5 | New tournament duplicates a name+edition the organiser already owns       | Reject and offer the existing tournament for selection instead.                                  |
| EC-6 | Generated join token collides with an existing one                        | Regenerate transparently; after repeated failures, fail the creation rather than reuse a token.  |
| EC-7 | Unauthenticated visitor posts directly to the pool creation endpoint      | Reject with an authentication challenge; no pool is created.                                     |
| EC-8 | Scoring-rules creation fails after the pool row is written                | The whole creation is rolled back (NFR-005); no orphan pool without rules survives.              |
| EC-9 | Organiser's display name is blank or already taken on their account       | Fall back to the local part of their email, suffixed if needed, so FR-007 always yields a name.   |
| EC-10 | Organiser renames a pool to the name it already has                       | Accept as a no-op; do not raise a duplicate-name error against the pool itself.                  |

## 8. Success Criteria

| ID     | Criterion                                                                                       |
| ------ | ----------------------------------------------------------------------------------------------- |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                       |
| SC-002 | An organiser can go from signed in to a created pool in under 30 seconds on first use.           |
| SC-003 | Every pool in the database has exactly one organiser player and exactly one set of scoring rules. |
| SC-004 | No pool is ever readable or renameable by a user who is not its organiser.                       |
| SC-005 | Join tokens are unique across all pools and show no detectable pattern.                          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Authentication**: this feature assumes registration, login and session management
  already exist and expose an authenticated `UserAccount`. No story in the story map covers
  this yet — see Open Question 1.
- **[Spec 002 — Enter the match schedule](002-enter-match-schedule.md)**: consumes the
  `Tournament` created here. FR-015 hands straight off to it.
- **[Spec 003 — Choose the scoring rules](003-scoring-rules.md)**: owns the `ScoringRules`
  attributes and the defaults installed by FR-008.
- **Story #004 (join link)**: consumes the `JoinToken` generated by FR-009.

### 9.2 Constraints

- The schedule is entered by hand (decision for [spec 002](002-enter-match-schedule.md)),
  so a tournament is an organiser-owned record rather than a shared reference list. Two
  organisers entering the same real-world tournament produce two independent `Tournament`
  rows. This is accepted for now; see §11 risk note in
  [docs/architecture/11-risks-and-technical-debt.md](../architecture/11-risks-and-technical-debt.md).
- One organiser per pool, fixed at creation. Shared administration is out of scope.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                            |
| -------------------------------- | ------------------------------------------------------------------------------------ |
| 3. Context & Scope               | The organiser is the primary external actor; no external systems are involved.        |
| 5. Building Block View           | Introduces the Pool, Tournament and Player building blocks and the identity boundary. |
| 6. Runtime View                  | The create-pool interaction, including the atomic pool + rules + player write.        |
| 8. Crosscutting Concepts         | Authentication and authorisation, validation, secure token generation.                |
| 9. Architecture Decisions (ADRs) | Account-based identity for organisers; organiser-owned tournaments.                   |
| 10. Quality Requirements         | Source of the project-wide performance and security scenarios NFR-001..004 refine.    |

## 10. Open Questions

| #   | Question                                                                                                      | Owner      | Status | Resolution |
| --- | ------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Account registration and login are assumed here but have no story in the story map. Add a story, or treat auth as infrastructure specified in arc42 §8? | Christophe | Open   |            |
| 2   | Should an organiser be able to delete a pool they created by mistake, and what happens to its players and predictions? | Christophe | Open   |            |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (0 present)
  - [x] Open questions are assigned and have a resolution path
-->
