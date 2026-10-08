# Feature Specification: Predictions Lock at Kickoff

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
| Feature ID      | 007                                                            |
| Status          | Draft                                                          |
| Author          | Christophe                                                     |
| Created         | 2026-10-08                                                     |
| Last updated    | 2026-10-08                                                     |
| Epic / Parent   | [Story map — Get everyone in](../product/story-map.md)         |
| Arc42 reference | 5. Building Block View, 6. Runtime View, 8. Crosscutting Concepts, 9. Decisions, 10. Quality Requirements |

### 1.1 Problem Statement

A prediction pool only works if everyone believes nobody can predict after the fact. If a
player can edit a scoreline once the ball is rolling — or even plausibly be suspected of it
— the leaderboard stops meaning anything and the group stops playing. The organiser must
not have to police this by hand, and the rule must hold without anyone having to be online
at kickoff.

### 1.2 Goal

Every prediction becomes immutable the instant its match kicks off, judged by the server
clock against the match's stored kickoff. Nothing is accepted after that moment, by anyone,
through any route — and at that same moment the match's predictions become visible to the
whole pool, so the fairness is something players can see rather than just trust.

### 1.3 Non-Goals

- The prediction form, validation and batch saving — [spec 006](006-enter-predictions.md).
- Scoring locked predictions — stories #008 and #009.
- Reminding players before the lock bites — story #010. This spec only warns on screen.
- Grace periods, late entry with a points penalty, or an organiser override that reopens a
  match. The lock is absolute.
- Deciding the kickoff instant. The organiser enters it in
  [spec 002](002-enter-match-schedule.md); this spec only reads it.

## 2. User Stories

### US-001: Trust the leaderboard

**As a** player,
**I want** everyone's predictions frozen at kickoff,
**so that** nobody can predict a match they have already watched.

### US-002: See that nobody cheated

**As a** player,
**I want** to see every player's prediction for a match once it has kicked off,
**so that** I can verify for myself that the pool is honest.

### US-003: Not be caught out by the deadline

**As a** player,
**I want** to be told clearly when a match locks and when it is about to,
**so that** I lose points to bad judgement, not to a surprise deadline.

### US-004: Stop policing the pool

**As an** organiser,
**I want** the lock to happen on its own,
**so that** I never have to close a round manually or adjudicate a late entry.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                     | Priority | User Story     |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------- |
| FR-001 | The system shall treat a prediction as locked when its match's kickoff instant is at or before the current server time.                           | Must     | US-001         |
| FR-002 | The system shall refuse to create a prediction for a locked match.                                                                               | Must     | US-001         |
| FR-003 | The system shall refuse to change a prediction for a locked match.                                                                               | Must     | US-001         |
| FR-004 | The system shall refuse to delete a prediction for a locked match.                                                                               | Must     | US-001         |
| FR-005 | The system shall enforce the lock on every write path, including direct submissions that bypass the prediction screen.                            | Must     | US-001         |
| FR-006 | The system shall apply the lock to every player of the pool, including the organiser.                                                            | Must     | US-001, US-004 |
| FR-007 | The system shall lock each match independently, so one match kicking off never affects predictions for a later one.                               | Must     | US-001         |
| FR-008 | The system shall lock without any scheduled job, timer or manual action — the lock follows from the kickoff instant alone.                        | Must     | US-004         |
| FR-009 | The system shall state, when it refuses a write, that the match locked at kickoff and give that kickoff time.                                     | Must     | US-003         |
| FR-010 | The system shall show each upcoming match's lock moment in the player's local time on the prediction screen.                                      | Must     | US-003         |
| FR-011 | The system shall present a locked match read-only, with no editable inputs.                                                                      | Must     | US-003         |
| FR-012 | The system shall reveal every player's prediction for a match to all players of the pool once that match is locked.                               | Must     | US-002         |
| FR-013 | The system shall keep every player's prediction private until the match is locked, per [spec 006](006-enter-predictions.md) FR-015.                | Must     | US-001, US-002 |
| FR-014 | The system shall show, for a locked match, which players of the pool submitted no prediction.                                                     | Should   | US-002         |
| FR-015 | The system shall count down to the lock on the prediction screen when a match kicks off within the next hour.                                     | Should   | US-003         |
| FR-016 | The system shall lock a match immediately if the organiser moves its kickoff to a past instant, and unlock it if the kickoff moves back to the future, except where predictions have already been revealed. | Should | US-004 |
| FR-017 | The system shall record, for audit, every write attempt it refused because of the lock.                                                          | Could    | US-001         |

## 4. Acceptance Scenarios

### SC-001: A prediction cannot be changed after kickoff (FR-001, FR-003, FR-009)

```gherkin
Given I predicted 2-1 for a match whose kickoff was ten minutes ago
When I try to change my prediction to 1-1
Then my prediction is still 2-1
  And I am told the match locked at its kickoff
  And I am shown that kickoff time
```

### SC-002: A missed match cannot be predicted late (FR-002)

```gherkin
Given I did not predict a match whose kickoff has passed
When I try to submit a prediction for it
Then no prediction is saved
  And I am told the match locked at its kickoff
```

### SC-003: The lock bites exactly at kickoff (FR-001)

```gherkin
Given a match kicks off at 20:00
When a prediction is submitted at 19:59:59 server time
Then it is saved
  And when a prediction is submitted at 20:00:00 server time
Then it is refused as locked
```

### SC-004: Other matches stay open (FR-007)

```gherkin
Given a pool has a match that kicked off an hour ago and a match kicking off tomorrow
When I open my predictions
Then the match from an hour ago is read-only
  And I can still enter and change a prediction for tomorrow's match
```

### SC-005: The organiser is not exempt (FR-006)

```gherkin
Given I am the organiser of a pool
  And one of its matches kicked off an hour ago
When I try to change my prediction for that match
Then my prediction is unchanged
  And I am told the match locked at its kickoff
```

### SC-006: The back door is shut too (FR-005)

```gherkin
Given a match that kicked off an hour ago
When a prediction for it is submitted directly, without using the prediction screen
Then it is refused as locked
  And nothing is stored
```

### SC-007: Predictions are revealed at kickoff (FR-012)

```gherkin
Given a pool has four players who all predicted a match
  And that match has kicked off
When I open the match as any player of the pool
Then I see all four predicted scorelines with their players' display names
```

### SC-008: Nothing is revealed before kickoff (FR-013)

```gherkin
Given a pool has four players who all predicted a match kicking off tomorrow
When I open that match
Then I see only my own prediction
  And I cannot tell what anyone else predicted
```

### SC-009: The screen shows the deadline (FR-010, FR-011)

```gherkin
Given I am a player in a pool with one upcoming and one kicked-off match
When I open my predictions
Then the upcoming match shows the moment it will lock, in my local time
  And the kicked-off match shows no editable input
```

### SC-010: Non-predictors are named after kickoff (FR-014)

```gherkin
Given a pool of five players, of whom two did not predict a match
  And that match has kicked off
When I open the match
Then I see the three submitted predictions
  And I see which two players submitted none
```

### SC-011: An imminent lock is counted down (FR-015)

```gherkin
Given a match kicks off in 25 minutes
When I open my predictions
Then that match shows how long is left before it locks
```

### SC-012: Moving a kickoff into the past locks the match (FR-016)

```gherkin
Given a match whose kickoff is tomorrow and which I have predicted
When the organiser moves its kickoff to an hour ago
Then my prediction for that match is locked
  And its predictions are revealed to the pool
```

## 5. Domain Model

This feature introduces no entity of its own. It defines a derived state over `Prediction`
and `Match`, both owned elsewhere.

### 5.1 Entities

#### Prediction

Owned by [spec 006](006-enter-predictions.md). This spec governs when it may be written.
`IsLocked` is **derived, not stored**: it is the comparison of the match's `kickoffAt`
against the server clock, so no process has to flip a flag and no prediction can be left in
a stale state.

| Attribute | Type    | Constraints                              | Description                                                   |
| --------- | ------- | ---------------------------------------- | ------------------------------------------------------------- |
| isLocked  | boolean | derived, never persisted                 | True when `match.kickoffAt <= now` on the server clock.        |

#### Match

Owned by [spec 002](002-enter-match-schedule.md). Its `kickoffAt`, stored as an absolute
instant, is the single source of the lock moment.

#### LockRejection (optional, FR-017)

An audit record of a write refused because the match was locked.

| Attribute   | Type     | Constraints                   | Description                                    |
| ----------- | -------- | ----------------------------- | ---------------------------------------------- |
| id          | UUID     | PK, generated                 |                                                |
| playerId    | UUID     | required, FK → Player         | Who attempted the write.                        |
| matchId     | UUID     | required, FK → Match          | Which match was locked.                         |
| attemptedAt | datetime | generated, immutable          | Server time of the attempt.                     |

### 5.2 Relationships

- A **Prediction**'s lock state is derived from its **Match**'s `kickoffAt`. Nothing else
  influences it — not the pool, not the scoring rules, not the result.
- A **Match** locks all of its **Predictions** simultaneously, across every pool following
  its tournament.
- Revelation (FR-012) is scoped to a **Pool**: a locked match shows the predictions of the
  players in the pool being viewed, not those of other pools following the same tournament.

### 5.3 Value Objects

#### LockState

The derived editability of a prediction, reported to the prediction screen.

| Attribute  | Type     | Constraints                                               |
| ---------- | -------- | --------------------------------------------------------- |
| state      | enum     | one of: `Open`, `ClosingSoon`, `Locked`                     |
| locksAt    | datetime | the match's `kickoffAt`, always present                     |
| timeLeft   | duration | ≥ 0 when `Open` or `ClosingSoon`, absent when `Locked`      |

`ClosingSoon` means `locksAt` is within one hour of now (FR-015).

### 5.4 Domain Rules and Invariants

- **Kickoff is the lock moment**: a prediction is editable strictly before its match's
  `kickoffAt` and never at or after it. The boundary instant itself is locked.
- **The lock is derived, never stored**: there is no flag to set and therefore no window in
  which a prediction is stale, no job that can fail, and no state to repair.
- **The server clock decides**: lock decisions compare stored absolute instants against the
  server's time. A client clock never influences the outcome.
- **The lock is universal**: it applies to every player including the organiser, through
  every route including direct submissions, with no override and no grace period.
- **The lock is per match**: locking is never a round-level or pool-level operation.
- **Locked means immutable, not deleted**: a locked prediction is fully readable; it simply
  cannot change.
- **Locked implies revealed**: within a pool, the moment a match locks, its predictions
  become visible to every player of that pool. Privacy and editability end together, which
  is what makes the guarantee checkable.
- **Revelation is irreversible**: once a match's predictions have been shown, they stay
  shown, even if its kickoff is later moved into the future (FR-016).
- **No prediction may record a change after its lock**: for every prediction,
  `submittedAt < kickoffAt` and, where present, `updatedAt < kickoffAt`.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                           |
| ------- | ------------ | ----------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Correctness  | The lock check is evaluated server-side on every write, within the same transaction that persists the prediction.        |
| NFR-002 | Correctness  | Kickoff instants and the current time are compared as absolute instants (UTC); no comparison involves a local-time string. |
| NFR-003 | Reliability  | The lock holds with no background job running; an outage of any scheduler cannot let a late prediction through.          |
| NFR-004 | Security     | No interface, including error messages and list endpoints, discloses another player's prediction for an unlocked match.  |
| NFR-005 | Performance  | Determining lock state adds no additional data access beyond the match already loaded for the screen.                    |
| NFR-006 | Usability    | The lock moment is visible for every upcoming match without the player taking any action to reveal it.                   |
| NFR-007 | Auditability | Every refusal carries the kickoff instant that caused it, so a disputed refusal can be explained precisely.              |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                            | Expected Behavior                                                                                          |
| ----- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| EC-1  | A submission arrives in the same second as kickoff                                   | Locked. The comparison is `kickoffAt <= now`, so the boundary instant belongs to the locked side.              |
| EC-2  | Kickoff passes while a multi-match form is open                                      | Only that match is refused; the rest of the batch saves (spec 006 FR-009).                                     |
| EC-3  | The match starts late in reality, or is delayed by weather                           | Irrelevant: the lock follows the scheduled `kickoffAt`, not the actual start. The organiser may move kickoff.   |
| EC-4  | The organiser moves a kickoff later, after the original moment passed but before predictions were revealed | The match unlocks (FR-016) and editing resumes until the new moment.                         |
| EC-5  | The organiser moves a kickoff later after predictions were revealed                  | Predictions stay revealed and stay locked; revelation is irreversible.                                         |
| EC-6  | The organiser moves a kickoff earlier, into the past                                 | The match locks at once; predictions already saved stand, and none can be added (SC-012).                       |
| EC-7  | The server clock is adjusted backwards                                               | A revealed match stays revealed and locked; the derived state otherwise follows the corrected clock.            |
| EC-8  | A match is added with a kickoff already in the past                                  | It is locked on creation; nobody can predict it. Spec 002 warns the organiser.                                  |
| EC-9  | A player joins a pool after some matches have locked                                 | They can read those matches' revealed predictions but hold none themselves and score nothing for them.          |
| EC-10 | Two pools follow the same tournament                                                 | The match locks for both at the same instant; each pool reveals only its own players' predictions.              |
| EC-11 | A player replays a previously captured write request after kickoff                   | Refused by FR-005; the lock lives in the domain, not in the screen.                                             |
| EC-12 | Nobody in the pool predicted a locked match                                          | The match is shown as locked with no predictions and every player listed as having missed it (FR-014).          |
| EC-13 | A match has no recorded result yet but has locked                                    | Normal: locking and settling are independent. Predictions are visible, points are not yet awarded.              |

## 8. Success Criteria

| ID     | Criterion                                                                                                             |
| ------ | ------------------------------------------------------------------------------------------------------------------------ |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                                              |
| SC-002 | No prediction in the database has `submittedAt` or `updatedAt` at or after its match's `kickoffAt`.                      |
| SC-003 | Every write path to a prediction is covered by a test that proves it refuses a locked match, including non-UI submissions. |
| SC-004 | No interface returns another player's prediction for an unlocked match, verified by test.                               |
| SC-005 | The system holds the lock correctly with every background process disabled.                                             |
| SC-006 | A player can state, from the prediction screen alone, exactly when each upcoming match stops accepting changes.          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 002 — Enter the match schedule](002-enter-match-schedule.md)**: owns `kickoffAt`,
  the only input to the lock, and the rules for changing it (FR-016).
- **[Spec 006 — Enter score predictions](006-enter-predictions.md)**: owns the `Prediction`
  this spec governs and defers to FR-001..004 on every write.
- **[Spec 005 — Join the pool](005-join-pool.md)**: supplies the pool membership that scopes
  revelation (FR-012) and the missing-predictor list (FR-014).
- **Story #008 (enter a result)**: operates only on locked predictions; a match cannot be
  settled before it has kicked off.

### 9.2 Constraints

- No grace period and no organiser override. The absoluteness of the rule is the product
  value; any exception would have to be re-argued from scratch.
- The lock is only as trustworthy as the kickoff instants the organiser entered. A wrong
  kickoff is a schedule problem, not a lock problem.
- Revelation at kickoff means a player who sees the first minutes of a match learns what
  everyone picked. That is accepted — nobody can act on it, since all those predictions are
  already locked.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                              |
| -------------------------------- | ---------------------------------------------------------------------------------------- |
| 5. Building Block View           | The lock is a domain rule on Prediction, not a component or a scheduled service.          |
| 6. Runtime View                  | Write → lock check inside the transaction → accept or refuse; reveal on read after kickoff. |
| 8. Crosscutting Concepts         | Time handling, the server clock as the single authority, authorisation, audit.            |
| 9. Architecture Decisions (ADRs) | Derived lock rather than a stored flag; reveal at kickoff; no grace period.                |
| 10. Quality Requirements         | Source of the fairness and correctness scenarios NFR-001..004 refine.                     |
| 11. Risks and Technical Debt     | Clock skew and mis-entered kickoff times as residual risks.                               |
| 12. Glossary                     | Lock, locked, revealed, kickoff.                                                          |

## 10. Open Questions

| #   | Question                                                                                                                    | Owner      | Status | Resolution |
| --- | ----------------------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Should predictions be revealed at that match's kickoff, or only once the whole round has kicked off?                          | Christophe | Open   |            |
| 2   | Should the organiser be prevented from moving a kickoff at all once any prediction exists, which would remove FR-016 entirely? | Christophe | Open   |            |

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
