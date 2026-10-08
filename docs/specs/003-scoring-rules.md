# Feature Specification: Choose the Scoring Rules

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
| Feature ID      | 003                                                                 |
| Status          | Draft                                                               |
| Author          | Christophe                                                          |
| Created         | 2026-10-08                                                          |
| Last updated    | 2026-10-08                                                          |
| Epic / Parent   | [Story map — Set up the pool](../product/story-map.md)              |
| Arc42 reference | 5. Building Block View, 8. Crosscutting Concepts, 9. Decisions       |

### 1.1 Problem Statement

Every friend group argues about what a prediction is worth. If the system picks the points
for them, the pool feels like someone else's game; if the rules can change mid-tournament,
the leaderboard stops being trustworthy. The organiser needs to set the stakes once, in the
open, before anything is at risk.

### 1.2 Goal

Each pool carries exactly one set of scoring rules, visible to every player. Scoring is
additive: getting the outcome right earns the pool's configured points, and getting the
exact score right earns one further bonus point on top. The organiser sets the outcome
points while the pool is still unstarted, after which the rules are frozen for the rest of
the tournament.

### 1.3 Non-Goals

- Per-stage multipliers, goal-difference tiers, joker matches, streak bonuses or any other
  scoring dimension. The model is deliberately two numbers, one of them fixed.
- Making the exact-score bonus configurable. It is always one point (see §5.4).
- Applying the rules to predictions and producing points — story #008 and the
  `PredictionScore` entity.
- Rendering the per-match points breakdown — story #009.
- Changing rules after the pool has started, or recalculating history if they did.
- Different rules for different players, or rules shared across pools.

## 2. User Stories

### US-001: Set the stakes for my pool

**As an** organiser,
**I want** to choose how many points a correct outcome is worth in my pool,
**so that** the scoring matches what my friend group agreed on.

### US-002: Know the rules before I predict

**As a** player,
**I want** to see my pool's scoring rules with a worked example,
**so that** I understand what my prediction is worth before I submit it.

### US-003: Trust that the rules will not move

**As a** player,
**I want** the scoring rules to be frozen once the tournament starts,
**so that** nobody can rewrite the stakes after seeing how the pool is going.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                      | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-001 | The system shall hold exactly one set of scoring rules per pool, created with the pool (see [spec 001](001-create-pool.md) FR-008).                | Must     | US-001     |
| FR-002 | The system shall default a new pool's correct-outcome points to 1.                                                                                 | Must     | US-001     |
| FR-003 | The system shall allow the organiser to set the correct-outcome points to any whole number from 1 to 100 inclusive.                                | Must     | US-001     |
| FR-004 | The system shall award a fixed bonus of 1 point for an exact score, which is displayed but not editable.                                           | Must     | US-001     |
| FR-005 | The system shall define a prediction's points as: correct-outcome points when the predicted outcome matches, plus 1 when the predicted score also matches exactly, and 0 otherwise. | Must     | US-001, US-002 |
| FR-006 | The system shall reject a correct-outcome value that is not a whole number, is below 1, or is above 100.                                           | Must     | US-001     |
| FR-007 | The system shall allow only the pool's organiser to change the scoring rules.                                                                      | Must     | US-001     |
| FR-008 | The system shall allow changes to the scoring rules only while the pool has not started, where "started" means the earliest kickoff in the pool's schedule is in the past. | Must     | US-003     |
| FR-009 | The system shall treat a pool whose schedule has no matches as not started, so its rules remain editable.                                           | Must     | US-003     |
| FR-010 | The system shall reject a change to the scoring rules of a started pool and shall state that the rules are locked.                                  | Must     | US-003     |
| FR-011 | The system shall show every player of the pool its scoring rules, read-only.                                                                       | Must     | US-002     |
| FR-012 | The system shall show, alongside the rules, a worked example of an exact hit, a correct outcome with the wrong score, and a wrong outcome, using the pool's current values. | Should   | US-002     |
| FR-013 | The system shall indicate to the organiser whether the rules are still editable and, if not, which match locked them.                               | Should   | US-003     |
| FR-014 | The system shall record when the scoring rules were last changed.                                                                                  | Could    | US-003     |

## 4. Acceptance Scenarios

### SC-001: A new pool starts with default rules (FR-001, FR-002, FR-004)

```gherkin
Given I am signed in as an organiser
When I create a pool
Then the pool has one set of scoring rules
  And its correct-outcome points are 1
  And its exact-score bonus is 1
```

### SC-002: Organiser raises the outcome points (FR-003)

```gherkin
Given I am the organiser of a pool whose schedule has no match that has kicked off
When I set the correct-outcome points to 3
Then the pool's scoring rules award 3 points for a correct outcome
  And the exact-score bonus is still 1
```

### SC-003: Scoring is additive (FR-005)

```gherkin
Given a pool whose correct-outcome points are 3 and whose exact-score bonus is 1
When the scoring rules are applied to a prediction of 2-0 against a result of 2-0
Then the prediction is worth 4 points
```

### SC-004: Right outcome, wrong score (FR-005)

```gherkin
Given a pool whose correct-outcome points are 3 and whose exact-score bonus is 1
When the scoring rules are applied to a prediction of 2-0 against a result of 3-1
Then the prediction is worth 3 points
```

### SC-005: Wrong outcome scores nothing (FR-005)

```gherkin
Given a pool whose correct-outcome points are 3 and whose exact-score bonus is 1
When the scoring rules are applied to a prediction of 2-0 against a result of 1-2
Then the prediction is worth 0 points
```

### SC-006: A predicted draw that ends in a draw (FR-005)

```gherkin
Given a pool whose correct-outcome points are 3 and whose exact-score bonus is 1
When the scoring rules are applied to a prediction of 1-1 against a result of 2-2
Then the prediction is worth 3 points
```

### SC-007: Out-of-range outcome points are rejected (FR-006)

```gherkin
Given I am the organiser of an unstarted pool whose correct-outcome points are 3
When I try to set the correct-outcome points to 0
Then the scoring rules are unchanged
  And I am told the value must be a whole number between 1 and 100
```

### SC-008: Rules lock once the pool has started (FR-008, FR-010)

```gherkin
Given I am the organiser of a pool whose earliest kickoff is in the past
When I try to change the correct-outcome points
Then the scoring rules are unchanged
  And I am told the rules are locked because the tournament has started
```

### SC-009: An empty schedule leaves the rules editable (FR-009)

```gherkin
Given I am the organiser of a pool whose schedule contains no matches
When I set the correct-outcome points to 5
Then the pool's scoring rules award 5 points for a correct outcome
```

### SC-010: Players can read but not change the rules (FR-007, FR-011)

```gherkin
Given I am a player in a pool but not its organiser
When I open the pool's scoring rules
Then I see the correct-outcome points and the exact-score bonus
  And I have no way to change them
```

### SC-011: Players see a worked example (FR-012)

```gherkin
Given I am a player in a pool whose correct-outcome points are 3
When I open the pool's scoring rules
Then I see that an exact score is worth 4 points
  And that a correct outcome with the wrong score is worth 3 points
  And that a wrong outcome is worth 0 points
```

### SC-012: The exact-score bonus cannot be edited (FR-004)

```gherkin
Given I am the organiser of an unstarted pool
When I attempt to change the exact-score bonus
Then the bonus is still 1
  And I am told the exact-score bonus is fixed
```

## 5. Domain Model

### 5.1 Entities

#### ScoringRules

The pool's point configuration. Exactly one per pool, created with the pool and frozen once
the pool's first match kicks off.

| Attribute             | Type     | Constraints                                      | Description                                                        |
| --------------------- | -------- | ------------------------------------------------ | ------------------------------------------------------------------ |
| id                    | UUID     | PK, generated                                    |                                                                    |
| poolId                | UUID     | required, FK → Pool, unique, immutable           | The pool these rules score.                                         |
| correctOutcomePoints  | integer  | required, 1..100, default 1                      | Awarded when the predicted win/draw/loss outcome matches.           |
| exactScoreBonusPoints | integer  | required, always 1, read-only                    | Added on top when the predicted score matches exactly.              |
| updatedAt             | datetime | nullable, set on each change                     | When the organiser last changed the rules (FR-014).                 |

### 5.2 Relationships

- A **Pool** has exactly one **ScoringRules**; **ScoringRules** belongs to exactly one
  **Pool** and is never shared.
- A **PredictionScore** (story #008) is produced by applying a pool's **ScoringRules** to a
  **Prediction** and its match's **MatchResult**.
- Whether the rules are editable is derived from the **Pool**'s **Tournament**'s earliest
  **Match** `kickoffAt` — it is not stored on **ScoringRules**.

### 5.3 Value Objects

#### Outcome

The win/draw/loss shape of a scoreline, derived from a pair of goal counts. Two scorelines
have the same outcome when their `Outcome` values are equal.

| Attribute | Type | Constraints                                  |
| --------- | ---- | -------------------------------------------- |
| value     | enum | one of: `HomeWin`, `Draw`, `AwayWin`          |

#### PointsAward

The result of applying the rules to one prediction: the total and the reason behind it, so
story #009 can show the breakdown.

| Attribute | Type    | Constraints                                                   |
| --------- | ------- | ------------------------------------------------------------- |
| points    | integer | ≥ 0                                                            |
| reason    | enum    | one of: `ExactScore`, `CorrectOutcome`, `NoPoints`              |

### 5.4 Domain Rules and Invariants

- **One rule set per pool**: a pool never has zero or two sets of scoring rules. Creation is
  atomic with the pool ([spec 001](001-create-pool.md) NFR-005).
- **Additive scoring**: `points = (outcome matches ? correctOutcomePoints : 0) + (score matches exactly ? 1 : 0)`.
- **An exact score always implies a correct outcome**: if the predicted score equals the
  result, the outcome necessarily matches, so an exact hit is always worth
  `correctOutcomePoints + 1` and the `ExactScore` reason subsumes `CorrectOutcome`.
- **Points are never negative**: the minimum award is 0 and the maximum is 101.
- **An exact hit always beats a mere correct outcome**: because the bonus is strictly
  positive, a player who nails the score never scores less than one who only called the
  winner.
- **The exact-score bonus is a system constant of 1**: it is stored on the rules so scores
  stay explainable, but no interface exposes it for editing.
- **Rules are frozen once the pool starts**: a pool is started when the earliest `kickoffAt`
  in its tournament's schedule is in the past. From that instant the rules are immutable,
  so no points already earned are ever revalued.
- **Rules apply uniformly**: the same rule set scores every match and every player in the
  pool, for the whole tournament.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                        |
| ------- | ------------ | ------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance  | Applying the rules to one prediction is a pure in-memory calculation requiring no further data access.               |
| NFR-002 | Performance  | The scoring rules screen renders in < 300 ms at p95.                                                                 |
| NFR-003 | Security     | Only the pool's organiser may change the rules; every player of the pool may read them; non-members may not read them. |
| NFR-004 | Correctness  | The scoring calculation is deterministic and total: every (prediction, result) pair yields exactly one `PointsAward`. |
| NFR-005 | Auditability | The rules in force are always displayable to players, so any leaderboard position can be explained from them.         |
| NFR-006 | Transparency | The lock state and its cause (FR-013) are visible to the organiser before they try to edit, not only on rejection.    |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                   | Expected Behavior                                                                                       |
| ----- | ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| EC-1  | Organiser submits a non-numeric, empty, decimal or negative outcome value                    | Reject with the range message (FR-006); the stored rules are unchanged.                                  |
| EC-2  | The pool's first kickoff passes between loading the edit form and submitting it              | Reject on submit (FR-010) and show the rules read-only with the match that locked them.                  |
| EC-3  | The organiser deletes the only match that had kicked off, via [spec 002](002-enter-match-schedule.md) | Not possible once predictions or results exist (spec 002 FR-015); if it is deletable, the pool reverts to unstarted and the rules unlock. |
| EC-4  | The organiser adds a match with a past kickoff to an unstarted pool                          | The pool becomes started immediately and the rules lock, consistent with FR-008.                         |
| EC-5  | Organiser sets the value to the number it already holds                                      | Accept as a no-op; `updatedAt` is not advanced.                                                           |
| EC-6  | Two tabs change the rules concurrently while the pool is unstarted                           | Last write wins; both writes are individually valid, so no conflict is surfaced.                          |
| EC-7  | A player who is not a member requests the pool's rules                                       | Report the pool as not found; do not disclose its existence or its rules.                                 |
| EC-8  | A prediction exists for a match with no recorded result                                      | No `PointsAward` is produced. Unsettled matches contribute nothing, not 0 by rule.                        |
| EC-9  | A result is recorded for a match no player predicted                                         | No awards are produced; this is not an error.                                                             |
| EC-10 | A prediction with a negative or absurd goal count reaches the calculation                    | Out of scope here — story #006 validates goal counts. The calculation must still be total and never throw. |
| EC-11 | Organiser sets the maximum of 100 points                                                     | Accepted; the maximum award per match becomes 101 and the leaderboard must handle it.                     |

## 8. Success Criteria

| ID     | Criterion                                                                                                   |
| ------ | ------------------------------------------------------------------------------------------------------------- |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                                    |
| SC-002 | The scoring calculation is covered by tests for every combination of outcome match and exact match.            |
| SC-003 | No pool in the database has zero or more than one set of scoring rules.                                        |
| SC-004 | No pool's scoring rules change after its first kickoff, verified by the `updatedAt` audit trail.               |
| SC-005 | Every player can state, from the rules screen alone, what their next prediction is worth in each of the three cases. |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 001 — Create a pool](001-create-pool.md)**: creates the `ScoringRules` row with
  the defaults in FR-002 and FR-004.
- **[Spec 002 — Enter the match schedule](002-enter-match-schedule.md)**: supplies the
  kickoff moments that decide whether the pool has started (FR-008).
- **Story #008 (enter a result, recalculate points)**: the consumer of FR-005; produces
  `PredictionScore` from a `PointsAward`.
- **Story #009 (per-match breakdown)**: consumes `PointsAward.reason`.

### 9.2 Constraints

- The exact-score bonus is fixed at 1 point by product decision. Making it configurable
  later is additive, but every pool created before that change keeps a bonus of 1.
- This replaces the three-knob `ScoringRules` (exact / outcome / bonus) sketched in the
  original story map and domain model. Both documents have been updated to match; anything
  referring to `ExactScorePoints` or a general `BonusPoints` is stale.
- Rules are frozen at first kickoff rather than at first prediction. An organiser can
  therefore still change the stakes after players have predicted, as long as nothing has
  kicked off — see Open Question 1.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                                  |
| -------------------------------- | ------------------------------------------------------------------------------------------ |
| 5. Building Block View           | ScoringRules sits under Pool; the scoring calculation is a pure domain service.              |
| 6. Runtime View                  | The edit-rules interaction and the started check that gates it.                              |
| 8. Crosscutting Concepts         | Validation, organiser-scoped authorisation, deriving "started" from schedule data.           |
| 9. Architecture Decisions (ADRs) | Additive scoring with a fixed exact-score bonus; freeze at first kickoff.                    |
| 10. Quality Requirements         | Source of the correctness and transparency scenarios NFR-004..006 refine.                    |
| 12. Glossary                     | Defines outcome, exact score, bonus, started and locked.                                     |

## 10. Open Questions

| #   | Question                                                                                                                             | Owner      | Status | Resolution |
| --- | -------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Should the rules freeze at the first kickoff, or earlier — at the first submitted prediction? Freezing at first kickoff lets the organiser move the goalposts after players have predicted. | Christophe | Open   |            |
| 2   | Is a maximum of 100 outcome points sensible, or should it be lower (say 10) to keep leaderboards readable?                              | Christophe | Open   |            |

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
