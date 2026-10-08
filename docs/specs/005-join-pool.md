# Feature Specification: Join a Pool and Pick a Display Name

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

| Field           | Value                                                          |
| --------------- | -------------------------------------------------------------- |
| Feature ID      | 005                                                            |
| Status          | Draft                                                          |
| Author          | Christophe                                                     |
| Created         | 2026-10-08                                                     |
| Last updated    | 2026-10-08                                                     |
| Epic / Parent   | [Story map — Get everyone in](../product/story-map.md)         |
| Arc42 reference | 3. Context & Scope, 5. Building Block View, 6. Runtime View, 8. Crosscutting Concepts |

### 1.1 Problem Statement

A friend taps a link in the group chat on their phone, probably minutes before kickoff.
If joining costs them a registration form, a confirmation e-mail and a profile page, they
will not finish. At the same time the pool needs each player to be a real, distinct identity
with a name the leaderboard can show, or the rivalry has nothing to point at.

### 1.2 Goal

Following a live join link turns a visitor into a player of that pool, with a display name
of their own choosing that is unique within the pool. The path is one screen: sign in (or
sign up), confirm the name, you are in and looking at the pool.

### 1.3 Non-Goals

- Producing or managing the link itself — [spec 004](004-invite-with-join-link.md).
- Entering predictions — [spec 006](006-enter-predictions.md). Joining lands the player on
  the pool, not in a prediction form.
- Avatars, profile pictures, bios or any other player profile content.
- Leaving a pool, being removed from one, or deleting an account.
- A global, cross-pool identity beyond the `UserAccount` already defined in
  [spec 001](001-create-pool.md). Display names are per pool.
- Identity provider details and session management — owned by the authentication feature
  referenced in §9.1.

## 2. User Stories

### US-001: Get into the pool from the chat

**As a** friend who was sent a join link,
**I want** following it to put me in the pool,
**so that** I can start predicting without filling in an admin form.

### US-002: Be known by the name I want

**As a** player,
**I want** to pick the display name I appear under in this pool,
**so that** the leaderboard shows the nickname my friend group actually calls me.

### US-003: Not be confused with someone else

**As a** player,
**I want** every name in the pool to be distinct,
**so that** I can always tell whose prediction and whose rank I am looking at.

### US-004: Not end up in the pool twice

**As a** player who opened the link again,
**I want** to be taken to the pool I am already in,
**so that** I never create a duplicate of myself.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                    | Priority | User Story     |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | -------------- |
| FR-001 | The system shall resolve a join link to its pool and show the pool's name, tournament and current player count on the join page.                 | Must     | US-001         |
| FR-002 | The system shall require the visitor to be authenticated before they become a player, offering sign-in or sign-up from the join page.             | Must     | US-001         |
| FR-003 | The system shall return the visitor to the same join link after they sign in or sign up, so the invitation is not lost.                          | Must     | US-001         |
| FR-004 | The system shall prompt the authenticated visitor for a display name, prefilled with their account's display name.                                | Must     | US-002         |
| FR-005 | The system shall accept a display name of 1 to 50 characters after trimming leading and trailing whitespace.                                     | Must     | US-002         |
| FR-006 | The system shall reject a display name that is empty, whitespace-only, or longer than 50 characters, and state the rule.                         | Must     | US-002         |
| FR-007 | The system shall reject a display name already used by another player in the same pool, compared case-insensitively, and say so.                  | Must     | US-003         |
| FR-008 | The system shall allow the same display name in different pools.                                                                                 | Must     | US-002         |
| FR-009 | The system shall create exactly one player for the pool on a successful join, linked to the visitor's user account, with `isOrganiser` false.     | Must     | US-001         |
| FR-010 | The system shall record when the player joined.                                                                                                  | Must     | US-001         |
| FR-011 | The system shall send the new player to the pool's screen after joining, with a confirmation that they are in.                                    | Must     | US-001         |
| FR-012 | The system shall send a visitor who is already a player of the pool straight to the pool's screen, creating nothing.                              | Must     | US-004         |
| FR-013 | The system shall refuse to create a player when the pool's joining is closed, or when the pool has started, and explain which applies.             | Must     | US-001         |
| FR-014 | The system shall refuse to create a player for an unrecognised or rotated-away join token, reporting the link as invalid.                         | Must     | US-001         |
| FR-015 | The system shall let a player change their display name within the pool, subject to the same rules, as long as the pool has not started.           | Should   | US-002         |
| FR-016 | The system shall update everywhere the player appears — leaderboard, predictions, comments — when their display name changes.                      | Should   | US-002         |
| FR-017 | The system shall show the new player the pool's scoring rules on arrival, as defined in [spec 003](003-scoring-rules.md) FR-011.                   | Should   | US-001         |
| FR-018 | The system shall suggest an available alternative when the chosen display name is taken.                                                          | Could    | US-003         |

## 4. Acceptance Scenarios

### SC-001: A friend joins from the link (FR-001, FR-004, FR-009, FR-011)

```gherkin
Given I am signed in and not a member of the pool "Vrienden EK"
  And I have a live join link for that pool
When I open the link and accept the prefilled display name
Then I am a player of "Vrienden EK"
  And I am on the pool's screen
  And I am told I have joined
```

### SC-002: Signing up keeps the invitation (FR-002, FR-003)

```gherkin
Given I am not signed in
  And I have a live join link for a pool
When I open the link and create an account
Then I am returned to the same join link
  And I can complete joining that pool
```

### SC-003: The join page says what I am joining (FR-001)

```gherkin
Given I have a live join link for the pool "Vrienden EK" following "Europees Kampioenschap 2028"
When I open the link
Then I see the pool name "Vrienden EK"
  And the tournament "Europees Kampioenschap 2028"
  And how many players have already joined
```

### SC-004: I pick my own nickname (FR-004, FR-005)

```gherkin
Given I am signed in with the account display name "Christophe"
  And I have a live join link for a pool
When I open the link and set my display name to "De Kenner"
Then I join the pool as "De Kenner"
  And the leaderboard shows me as "De Kenner"
```

### SC-005: A taken name is refused (FR-007)

```gherkin
Given a pool already has a player called "De Kenner"
  And I am signed in and hold a live join link for that pool
When I try to join as "de kenner"
Then I am not added to the pool
  And I am told that name is already taken in this pool
```

### SC-006: An empty name is refused (FR-006)

```gherkin
Given I am signed in and hold a live join link for a pool
When I try to join with a blank display name
Then I am not added to the pool
  And I am told the display name must be 1 to 50 characters
```

### SC-007: The same name works in another pool (FR-008)

```gherkin
Given I am a player called "De Kenner" in the pool "Vrienden EK"
  And I hold a live join link for the pool "Collega's EK"
When I join as "De Kenner"
Then I am a player in both pools under that name
```

### SC-008: Opening the link twice does not duplicate me (FR-012)

```gherkin
Given I am already a player in a pool
When I open its join link again
Then I am taken to the pool's screen
  And the pool still has the same number of players
```

### SC-009: A closed pool turns me away (FR-013)

```gherkin
Given I am signed in and hold a join link for a pool whose joining is closed
When I open the link
Then I am told the pool is not accepting new players
  And I am not a player of that pool
```

### SC-010: A started pool turns me away (FR-013)

```gherkin
Given I am signed in and hold a join link for a pool whose earliest kickoff has passed
When I try to join
Then I am told the tournament has already started
  And I am not a player of that pool
```

### SC-011: A rotated link is dead (FR-014)

```gherkin
Given I hold a join link whose token has since been rotated
When I open the link
Then I am told the link is no longer valid
  And I learn nothing else about the pool
```

### SC-012: Renaming before kickoff (FR-015, FR-016)

```gherkin
Given I am a player called "De Kenner" in a pool that has not started
When I change my display name to "De Echte Kenner"
Then the pool's leaderboard and my predictions show me as "De Echte Kenner"
  And no other record of "De Kenner" remains in the pool
```

### SC-013: Renaming to a taken name is refused (FR-015, FR-007)

```gherkin
Given I am a player in a pool that also contains a player called "Peter"
When I try to change my display name to "Peter"
Then my display name is unchanged
  And I am told that name is already taken in this pool
```

## 5. Domain Model

### 5.1 Entities

#### Player

A participant in one pool, identified by their display name within that pool and backed by
a user account. Introduced in [spec 001](001-create-pool.md) for the organiser; this spec
owns the join path that creates everyone else.

| Attribute   | Type     | Constraints                                               | Description                                        |
| ----------- | -------- | --------------------------------------------------------- | -------------------------------------------------- |
| id          | UUID     | PK, generated                                             |                                                    |
| poolId      | UUID     | required, FK → Pool, immutable                            | The pool this membership is in.                     |
| userId      | UUID     | required, FK → UserAccount, immutable, unique with poolId | The account behind the player.                      |
| displayName | string   | required, 1–50 chars trimmed, unique within pool (ci)      | How the player appears in this pool.                |
| isOrganiser | boolean  | required, false for joined players                         | True for exactly one player per pool.               |
| joinedAt    | datetime | generated, immutable                                       | When the player joined (FR-010).                    |
| renamedAt   | datetime | nullable, set on each rename                               | When the display name was last changed (FR-015).    |

#### UserAccount

Owned by [spec 001](001-create-pool.md). This feature reads `displayName` to prefill FR-004
and creates no account itself — sign-up belongs to the authentication feature (§9.1).

### 5.2 Relationships

- A **Pool** has many **Players**; a **Player** belongs to exactly one **Pool**.
- A **Player** is backed by exactly one **UserAccount**; a **UserAccount** may be a
  **Player** in many **Pools**, but at most once per pool.
- A **Player** is the author of many **Predictions** ([spec 006](006-enter-predictions.md)),
  many **Comments** (#013), and has one **LeaderboardEntry** per pool (#011).
- Joining consumes a **JoinLink** ([spec 004](004-invite-with-join-link.md)) but creates no
  relationship to it — the link is not recorded on the player.

### 5.3 Value Objects

#### DisplayName

The name a player carries inside one pool.

| Attribute | Type   | Constraints                                                                |
| --------- | ------ | --------------------------------------------------------------------------- |
| value     | string | required, 1–50 chars after trimming, unique per pool compared case-insensitively |

### 5.4 Domain Rules and Invariants

- **One membership per account per pool**: a `UserAccount` is a `Player` at most once in a
  given pool. Re-following the link is idempotent (FR-012).
- **Display name is unique within a pool**: two players in the same pool never share a name,
  compared case-insensitively, so the leaderboard is never ambiguous.
- **Display names are pool-scoped**: the same person may be "De Kenner" in one pool and
  "Christophe" in another; neither constrains the other.
- **Authentication precedes membership**: a player always has a user account behind them.
  There are no anonymous players.
- **Joining never changes the account**: picking a pool display name does not alter
  `UserAccount.displayName`, which only ever serves as the prefill.
- **Exactly one organiser**: joining never produces a second organiser; `isOrganiser` is
  false for every player created by this feature.
- **No joining after kickoff**: a pool that has started admits nobody, so every player in
  the leaderboard had the same opportunity to predict.
- **Renaming preserves identity**: a rename changes the label, never the player's id,
  predictions, points or rank.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                        |
| ------- | ------------ | -------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Usability    | From opening the link, an already-signed-in visitor joins in at most two actions (confirm name, submit).              |
| NFR-002 | Usability    | The join page is usable on a mobile browser at 360 px width without horizontal scrolling.                             |
| NFR-003 | Performance  | The join page renders in < 300 ms at p95; joining completes in < 500 ms at p95, measured server-side.                 |
| NFR-004 | Security     | A visitor without a valid token learns nothing about any pool, including whether it exists.                           |
| NFR-005 | Security     | The join page discloses only the pool name, tournament and player count — never the member list, predictions or scores. |
| NFR-006 | Reliability  | Joining is atomic and idempotent: concurrent submissions from the same account produce exactly one player.             |
| NFR-007 | Reliability  | Display-name uniqueness is enforced at the storage level, not only in the form, so concurrent joins cannot both win.   |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                      | Expected Behavior                                                                                  |
| ----- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| EC-1  | Two visitors submit the same display name at the same moment                    | One join succeeds; the other is rejected with the taken-name message and keeps their entered values.   |
| EC-2  | The organiser closes joining between loading the join page and submitting it     | Reject on submit (FR-013); show the pool-closed message, create nothing.                               |
| EC-3  | The organiser rotates the token between loading and submitting                   | Reject on submit as an invalid link (FR-014).                                                          |
| EC-4  | The pool's first kickoff passes between loading and submitting                   | Reject on submit as a started pool (FR-013).                                                           |
| EC-5  | The display name differs only by case or surrounding whitespace from an existing one | Treated as a duplicate; trimming happens before the comparison.                                      |
| EC-6  | The display name contains emoji, accents or right-to-left text                   | Accepted; the 50-character limit counts user-perceived characters, and the value is rendered escaped.  |
| EC-7  | The display name contains HTML or script-like text                               | Stored verbatim and always rendered escaped; never interpreted as markup.                              |
| EC-8  | The organiser opens their own pool's join link                                   | Taken to the pool (FR-012); they are already a player and stay the organiser.                          |
| EC-9  | The visitor's account display name is longer than 50 characters                  | The prefill is truncated to 50 characters; the visitor may edit it before submitting.                  |
| EC-10 | The visitor abandons the join page after signing in                              | No player is created. Reaching the page never implies membership.                                      |
| EC-11 | The visitor submits twice quickly (double tap)                                   | Exactly one player exists; the second submission resolves to FR-012.                                   |
| EC-12 | A player renames after the pool has started                                      | Rejected (FR-015); the name is frozen so historical standings stay readable.                           |
| EC-13 | A signed-in visitor follows a link for a pool they organise but which is closed  | Taken to the pool; closing never affects existing members (spec 004 FR-011).                           |

## 8. Success Criteria

| ID     | Criterion                                                                                                   |
| ------ | ------------------------------------------------------------------------------------------------------------- |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                                    |
| SC-002 | A new user, starting from a link on a phone, is a player within one minute including sign-up.                  |
| SC-003 | No pool in the database contains two players with the same user account or the same display name (ci).         |
| SC-004 | Repeated submission of the same join never produces more than one player, verified under concurrent load.      |
| SC-005 | No join attempt against an invalid, rotated, closed or started pool creates a player.                          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 004 — Invite with a join link](004-invite-with-join-link.md)**: supplies the token,
  the joining state and the invalid-link semantics this spec enforces.
- **[Spec 001 — Create a pool](001-create-pool.md)**: defines `Pool`, `UserAccount` and the
  organiser `Player` row this feature mirrors.
- **[Spec 003 — Scoring rules](003-scoring-rules.md)**: the rules shown to the arriving
  player (FR-017).
- **Authentication feature**: sign-in, sign-up, session and the return-to-link behaviour in
  FR-003. Not specified here.
- **[Spec 002 — Enter the match schedule](002-enter-match-schedule.md)**: supplies the
  kickoff moments behind the started check in FR-013.

### 9.2 Constraints

- Display names are unique per pool, not globally. A global nickname registry is explicitly
  rejected: friend groups overlap and would fight over names.
- An account cannot hold two memberships in one pool, so a person cannot play twice under
  two aliases.
- Renaming stops at kickoff to keep past rounds attributable — see Open Question 1.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                      |
| -------------------------------- | -------------------------------------------------------------------------------- |
| 3. Context & Scope               | The anonymous join page is the only entry point reachable without membership.    |
| 5. Building Block View           | Player membership sits under Pool; the join page is a separate anonymous page.    |
| 6. Runtime View                  | Open link → authenticate → name → player created → pool screen.                   |
| 8. Crosscutting Concepts         | Authentication, validation, idempotency, output escaping, not-found-over-forbidden. |
| 9. Architecture Decisions (ADRs) | Pool-scoped display names; one membership per account per pool.                   |
| 10. Quality Requirements         | Source of the usability and reliability scenarios NFR-001..007 refine.            |
| 12. Glossary                     | Player, display name, join, organiser.                                           |

## 10. Open Questions

| #   | Question                                                                                                             | Owner      | Status | Resolution |
| --- | ---------------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Should renaming really stop at kickoff, or should a player be free to rename at any time since identity is stable anyway? | Christophe | Open   |            |
| 2   | Should the organiser be able to rename or remove a player whose display name is offensive?                             | Christophe | Open   |            |

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
