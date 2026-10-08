# Feature Specification: Enter Score Predictions

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
| Feature ID      | 006                                                            |
| Status          | Draft                                                          |
| Author          | Christophe                                                     |
| Created         | 2026-10-08                                                     |
| Last updated    | 2026-10-08                                                     |
| Epic / Parent   | [Story map — Get everyone in](../product/story-map.md)         |
| Arc42 reference | 5. Building Block View, 6. Runtime View, 8. Crosscutting Concepts, 10. Quality Requirements |

### 1.1 Problem Statement

Predicting is the thing players actually do, and they do it on a phone, in a hurry, usually
for several matches at once and often minutes before kickoff. If entering a scoreline takes
more than a few taps, or if a player cannot tell at a glance which matches they still owe,
predictions get missed and the pool dies of attrition rather than of bad football takes.

### 1.2 Goal

Every player can see the pool's open matches in kickoff order, type a predicted scoreline
for each, and save them — individually or as a round in one go. Saved predictions are
visible and editable right up to their match's kickoff, and the player always knows which
matches are still outstanding.

### 1.3 Non-Goals

- Locking predictions at kickoff — [spec 007](007-prediction-lock.md) owns the lock rule,
  its boundary conditions and its guarantees. This spec only honours it.
- Scoring predictions or showing points — stories #008 and #009.
- Reminding or nudging players who have not predicted — story #010.
- Predicting anything but the final scoreline: no first scorer, no outright winner, no
  joker matches, no confidence weighting.
- Predicting knockout fixtures before their teams are known. Matches appear when the
  organiser enters them ([spec 002](002-enter-match-schedule.md)).
- Seeing other players' predictions before kickoff.

## 2. User Stories

### US-001: Fill in a round in one sitting

**As a** player,
**I want** to enter scorelines for all the upcoming matches on one screen,
**so that** I can do a whole round in a couple of minutes on my phone.

### US-002: Change my mind before kickoff

**As a** player,
**I want** to edit a prediction I already saved,
**so that** I can react to an injury or a line-up announcement.

### US-003: Know what I still owe

**As a** player,
**I want** to see which upcoming matches I have not predicted,
**so that** I never lose points to forgetfulness.

### US-004: Predict in private

**As a** player,
**I want** my predictions hidden from everyone until kickoff,
**so that** nobody can copy me or play off what I picked.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                          | Priority | User Story     |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | -------------- |
| FR-001 | The system shall show a player every match in their pool's tournament whose kickoff is in the future, ordered by kickoff ascending.                    | Must     | US-001         |
| FR-002 | The system shall group the listed matches by date and show each match's teams, kickoff in the player's local time, and stage.                          | Must     | US-001         |
| FR-003 | The system shall let a player enter a predicted home-goal and away-goal count for any listed match.                                                    | Must     | US-001         |
| FR-004 | The system shall accept goal counts that are whole numbers from 0 to 99 inclusive.                                                                     | Must     | US-001         |
| FR-005 | The system shall reject a goal count that is empty, non-numeric, negative, fractional or above 99, and state the rule.                                  | Must     | US-001         |
| FR-006 | The system shall require both goal counts to be present to save a prediction; a half-filled scoreline is not saved.                                     | Must     | US-001         |
| FR-007 | The system shall hold at most one prediction per player per match.                                                                                     | Must     | US-001         |
| FR-008 | The system shall let a player save predictions for several matches in a single submission.                                                             | Must     | US-001         |
| FR-009 | The system shall save the valid predictions in a multi-match submission and report the invalid ones without discarding what the player typed.           | Must     | US-001         |
| FR-010 | The system shall let a player overwrite an existing prediction for a match whose kickoff is in the future.                                             | Must     | US-002         |
| FR-011 | The system shall record when a prediction was first submitted and when it was last changed.                                                            | Must     | US-002         |
| FR-012 | The system shall refuse to create or change a prediction for a match whose kickoff has passed, per [spec 007](007-prediction-lock.md).                 | Must     | US-002         |
| FR-013 | The system shall mark each listed match as predicted or not predicted for the signed-in player.                                                        | Must     | US-003         |
| FR-014 | The system shall show the player a count of upcoming matches they have not yet predicted.                                                              | Must     | US-003         |
| FR-015 | The system shall show no other player's prediction for a match before that match's kickoff.                                                            | Must     | US-004         |
| FR-016 | The system shall allow a prediction only from a player of the pool the match's tournament belongs to.                                                  | Must     | US-004         |
| FR-017 | The system shall confirm what was saved, naming the matches, after a successful submission.                                                            | Should   | US-001         |
| FR-018 | The system shall warn the player when a listed match kicks off within the next hour.                                                                   | Should   | US-003         |
| FR-019 | The system shall let a player also review their predictions for matches that have already kicked off, read-only.                                       | Should   | US-002         |
| FR-020 | The system shall preserve unsaved input when a submission is rejected for validation, so nothing has to be retyped.                                     | Should   | US-001         |
| FR-021 | The system shall show the pool's scoring rules alongside the prediction list, per [spec 003](003-scoring-rules.md) FR-011.                              | Could    | US-001         |

## 4. Acceptance Scenarios

### SC-001: The upcoming matches are listed in order (FR-001, FR-002)

```gherkin
Given I am a player in a pool whose tournament has three future matches and one past match
When I open my predictions
Then I see exactly the three future matches
  And they are ordered by kickoff, earliest first
  And each shows both teams, its stage and its kickoff time
```

### SC-002: Entering a scoreline (FR-003, FR-004, FR-007)

```gherkin
Given I am a player in a pool with an upcoming match België–Italië
When I predict 2-1 for that match and save
Then my prediction for België–Italië is 2-1
  And the match is marked as predicted
```

### SC-003: A whole round in one submission (FR-008, FR-017)

```gherkin
Given I am a player in a pool with four upcoming matches
When I enter a scoreline for all four and save once
Then I have four predictions
  And I am told all four were saved
```

### SC-004: Partial success keeps the good ones (FR-009, FR-020)

```gherkin
Given I am a player in a pool with three upcoming matches
When I enter 2-1, 1-1 and "3-x" and save
Then the first two predictions are saved
  And I am told the third is invalid
  And the values I typed are still in the form
```

### SC-005: Changing my mind (FR-010, FR-011)

```gherkin
Given I have predicted 2-1 for an upcoming match
When I change the prediction to 0-0 and save
Then my prediction for that match is 0-0
  And the pool still holds exactly one prediction from me for that match
  And the prediction records that it was changed
```

### SC-006: Half a scoreline is not a prediction (FR-006)

```gherkin
Given I am a player in a pool with an upcoming match
When I enter 2 for the home goals, leave the away goals empty and save
Then no prediction is saved for that match
  And I am told both scores are required
  And the match is still marked as not predicted
```

### SC-007: Out-of-range goals are refused (FR-005)

```gherkin
Given I am a player in a pool with an upcoming match
When I try to predict -1 goals for the home team
Then no prediction is saved for that match
  And I am told goals must be a whole number between 0 and 99
```

### SC-008: Kickoff has passed (FR-012)

```gherkin
Given I have predicted 2-1 for a match whose kickoff has since passed
When I try to change the prediction to 0-0
Then my prediction is still 2-1
  And I am told the prediction locked at kickoff
```

### SC-009: I can see what I still owe (FR-013, FR-014)

```gherkin
Given I am a player in a pool with five upcoming matches and I have predicted two of them
When I open my predictions
Then three matches are marked as not predicted
  And I am told I have three predictions outstanding
```

### SC-010: Predictions are private before kickoff (FR-015)

```gherkin
Given another player has predicted 3-0 for an upcoming match
When I open my predictions
Then I do not see their prediction anywhere
```

### SC-011: Non-members cannot predict (FR-016)

```gherkin
Given I am signed in but not a player of the pool
When I try to submit a prediction for one of its matches
Then no prediction is saved
  And the pool is reported as not found
```

### SC-012: An imminent kickoff is flagged (FR-018)

```gherkin
Given I am a player in a pool with a match kicking off in 40 minutes
When I open my predictions
Then that match is flagged as kicking off soon
```

### SC-013: Past predictions stay visible (FR-019)

```gherkin
Given I predicted 2-1 for a match that has already kicked off
When I review my past predictions
Then I see 2-1 for that match
  And I have no way to change it
```

## 5. Domain Model

### 5.1 Entities

#### Prediction

A player's predicted final scoreline for one match in their pool. At most one per player
per match; immutable once its match has kicked off
([spec 007](007-prediction-lock.md)).

| Attribute   | Type     | Constraints                                                 | Description                                           |
| ----------- | -------- | ----------------------------------------------------------- | ----------------------------------------------------- |
| id          | UUID     | PK, generated                                               |                                                       |
| playerId    | UUID     | required, FK → Player, immutable, unique with matchId        | The player who predicted.                              |
| matchId     | UUID     | required, FK → Match, immutable                              | The fixture predicted.                                 |
| homeGoals   | integer  | required, 0..99                                              | Predicted goals for the match's home team.             |
| awayGoals   | integer  | required, 0..99                                              | Predicted goals for the match's away team.             |
| submittedAt | datetime | generated, immutable                                         | When the prediction was first saved (FR-011).          |
| updatedAt   | datetime | nullable, set on each change                                 | When it was last changed (FR-011).                     |

#### Match

Owned by [spec 002](002-enter-match-schedule.md). This feature reads `kickoffAt`, `stage`
and the two teams; it never changes a match.

### 5.2 Relationships

- A **Player** submits many **Predictions**; a **Prediction** belongs to exactly one
  **Player**.
- A **Prediction** is for exactly one **Match**; a **Match** is the subject of many
  **Predictions**, at most one per player of each pool following its tournament.
- A **Prediction** earns at most one **PredictionScore** (story #008) once its match is
  settled. Nothing in this spec produces one.
- Whether a prediction is editable is derived from its **Match**'s `kickoffAt` — it is not
  stored on the prediction ([spec 007](007-prediction-lock.md)).

### 5.3 Value Objects

#### Scoreline

A predicted or actual pair of goal counts. Shared with `MatchResult` (story #008) so the
two can be compared directly when scoring.

| Attribute | Type    | Constraints      |
| --------- | ------- | ---------------- |
| homeGoals | integer | 0..99            |
| awayGoals | integer | 0..99            |

### 5.4 Domain Rules and Invariants

- **One prediction per player per match**: saving again overwrites; it never creates a
  second row, so a player can never hold two scorelines for one fixture.
- **A prediction is complete or absent**: both goal counts are always present. There is no
  prediction with only a home score.
- **Goals are non-negative whole numbers**: 0 ≤ goals ≤ 99 for both sides. A scoreline is
  never negative and never fractional.
- **A prediction belongs to its pool's tournament**: a prediction may only reference a match
  of the tournament followed by the pool the player belongs to.
- **Editable until kickoff**: a prediction may be created or changed at any time before its
  match's `kickoffAt`, and never after ([spec 007](007-prediction-lock.md)).
- **Predictions are private until kickoff**: before kickoff, only the author may read their
  own prediction. Matches, not pools, control this — each match reveals independently.
- **Missing is not zero**: a player who did not predict has no prediction, which is distinct
  from having predicted 0-0, and scores nothing rather than scoring as a draw.
- **Predictions survive renames**: a prediction follows the `Player`, so a display-name
  change ([spec 005](005-join-pool.md) FR-015) never affects it.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                          |
| ------- | ------------ | ---------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Usability    | Predicting one match takes at most three interactions (home goals, away goals, save) on a touch device.                 |
| NFR-002 | Usability    | The prediction list is usable on a mobile browser at 360 px width, with numeric keypads for goal inputs.                |
| NFR-003 | Performance  | The prediction list renders in < 400 ms at p95 for a tournament of up to 64 matches.                                    |
| NFR-004 | Performance  | A multi-match submission of up to 16 predictions completes in < 600 ms at p95.                                          |
| NFR-005 | Security     | A player may read and write only their own predictions; no interface returns another player's prediction before kickoff. |
| NFR-006 | Reliability  | A multi-match submission persists each valid prediction independently: a rejection of one never rolls back another.      |
| NFR-007 | Reliability  | Uniqueness of (player, match) is enforced at the storage level, so concurrent submissions cannot create duplicates.      |
| NFR-008 | Correctness  | Kickoff comparison uses the server's clock against the match's stored instant, never a client-supplied time.             |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                     | Expected Behavior                                                                                    |
| ----- | ------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------ |
| EC-1  | Kickoff passes between loading the form and submitting it                      | That match's prediction is rejected as locked; other matches in the same submission still save.          |
| EC-2  | The player submits an empty form                                               | Nothing is saved and nothing is reported as an error; existing predictions are untouched.                |
| EC-3  | The player clears a previously saved prediction's fields and saves              | The existing prediction is left as it was; predictions cannot be withdrawn — see Open Question 1.        |
| EC-4  | Goal counts are entered with leading zeros or surrounding whitespace            | Trimmed and parsed; "07" is accepted as 7.                                                               |
| EC-5  | Goal counts are entered in a non-Latin numeral system or with a decimal point   | Rejected with the range message (FR-005).                                                                |
| EC-6  | The pool's tournament has no matches yet                                       | Show an empty list explaining the organiser has not entered the schedule.                                |
| EC-7  | Every upcoming match has already been predicted                                | Show the list with zero outstanding and no nagging.                                                      |
| EC-8  | The organiser deletes a match that the player had predicted                    | The prediction goes with it (spec 002 FR-015 restricts this); the list no longer shows the fixture.      |
| EC-9  | The organiser changes a match's kickoff to a past moment                       | The match leaves the upcoming list and its predictions lock immediately.                                 |
| EC-10 | Two tabs save different scorelines for the same match                          | Last write wins; the prediction holds the later scoreline and `updatedAt` reflects it.                    |
| EC-11 | The player belongs to two pools following the same tournament                  | They hold one prediction per pool membership, since predictions hang off `Player`, not `UserAccount`.     |
| EC-12 | A knockout fixture is added hours before kickoff                               | It appears in the list immediately and is flagged as kicking off soon (FR-018).                           |
| EC-13 | The player's device clock is wrong                                             | Irrelevant: lock decisions use the server clock (NFR-008); only the displayed local time may look off.    |

## 8. Success Criteria

| ID     | Criterion                                                                                                       |
| ------ | ----------------------------------------------------------------------------------------------------------------- |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                                        |
| SC-002 | A player can enter a full group-stage round of 6 matches in under 90 seconds on a phone.                           |
| SC-003 | No (player, match) pair has more than one prediction in the database.                                             |
| SC-004 | No prediction exists with a goal count outside 0..99 or with only one side filled in.                             |
| SC-005 | No prediction's `updatedAt` is later than its match's `kickoffAt`.                                                 |
| SC-006 | No interface discloses another player's prediction for a match that has not kicked off, verified by test.          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 002 — Enter the match schedule](002-enter-match-schedule.md)**: supplies the
  matches, their teams, stages and kickoff instants.
- **[Spec 005 — Join the pool](005-join-pool.md)**: supplies the `Player` that owns a
  prediction.
- **[Spec 007 — Predictions lock at kickoff](007-prediction-lock.md)**: owns the lock rule
  this spec defers to in FR-012.
- **[Spec 003 — Scoring rules](003-scoring-rules.md)**: shown alongside the list (FR-021);
  defines what a scoreline is worth.
- **Story #008 (enter a result)**: consumes `Prediction` and its `Scoreline` to produce
  `PredictionScore`.

### 9.2 Constraints

- A prediction is a final scoreline only. For knockout matches this is the score at the end
  of normal time unless the organiser's result says otherwise — see Open Question 2.
- Predictions cannot be withdrawn once saved, only overwritten.
- Matches must exist before they can be predicted, so late-entered knockout fixtures give
  players less time. That is a consequence of the no-placeholder-teams decision in spec 002.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                        |
| -------------------------------- | ---------------------------------------------------------------------------------- |
| 5. Building Block View           | Prediction sits under Player; the prediction list reads the tournament schedule.    |
| 6. Runtime View                  | List → multi-match submit → partial-success response.                               |
| 8. Crosscutting Concepts         | Validation, time handling and the server clock, per-player authorisation, escaping. |
| 9. Architecture Decisions (ADRs) | One prediction per player per match; partial-success batch saves; scoreline-only predictions. |
| 10. Quality Requirements         | Source of the usability and performance scenarios NFR-001..004 refine.              |
| 12. Glossary                     | Prediction, scoreline, outstanding, round.                                          |

## 10. Open Questions

| #   | Question                                                                                                                      | Owner      | Status | Resolution |
| --- | ------------------------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Should a player be able to delete a prediction outright, or is overwrite-only correct?                                          | Christophe | Open   |            |
| 2   | For knockout matches, is the predicted scoreline the result after normal time or after extra time and penalties?                | Christophe | Open   |            |

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
