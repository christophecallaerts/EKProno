# Feature Specification: Enter the Match Schedule

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
| Feature ID      | 002                                                                 |
| Status          | Draft                                                               |
| Author          | Christophe                                                          |
| Created         | 2026-10-08                                                          |
| Last updated    | 2026-10-08                                                          |
| Epic / Parent   | [Story map — Set up the pool](../product/story-map.md)              |
| Arc42 reference | 3. Context & Scope, 5. Building Block View, 8. Crosscutting Concepts |

### 1.1 Problem Statement

A pool without fixtures is an empty shell: there is nothing to predict, nothing to lock at
kickoff and nothing to score. EKProno has no connection to any football data provider, so
the tournament's teams and fixtures only exist once the organiser types them in.

### 1.2 Goal

The organiser can build their tournament's schedule by hand — adding teams, then adding
matches with a home side, an away side, a kickoff moment and a stage — and can correct it
as the tournament unfolds. Knockout fixtures are added once their teams are known, so the
schedule grows over the course of the tournament rather than being complete on day one.

### 1.3 Non-Goals

- Importing a schedule from an external football data API, a file or a paste. Every match
  is entered through the UI. Import is a likely follow-up (see §9.2).
- Placeholder knockout fixtures such as "Winner Group A vs Runner-up Group B". A knockout
  match is only created once both teams are decided.
- Group tables, qualification logic or deriving who advances. The organiser decides which
  teams meet in a knockout match; the system does not compute it.
- Entering match results — story #008.
- Predictions and prediction locking — stories #006 and #007, which consume `Match.kickoffAt`.
- Sharing teams or fixtures between tournaments owned by different organisers.

## 2. User Stories

### US-001: Register the participating teams

**As an** organiser,
**I want** to add the teams taking part in my tournament,
**so that** I can build fixtures from them without retyping team names.

### US-002: Add group-stage fixtures

**As an** organiser,
**I want** to add each group-stage match with its two teams, kickoff moment and stage,
**so that** my players have something to predict from the moment they join.

### US-003: Add knockout fixtures as they are decided

**As an** organiser,
**I want** to add a knockout match once I know which two teams play it,
**so that** the schedule keeps up with the tournament without me inventing placeholders.

### US-004: Correct a mistake in the schedule

**As an** organiser,
**I want** to edit or remove a fixture I entered wrongly,
**so that** a typo in a kickoff time does not lock predictions at the wrong moment.

### US-005: Review the schedule

**As an** organiser,
**I want** to see the full schedule in kickoff order with its stages,
**so that** I can check at a glance what is still missing.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                      | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ---------- |
| FR-001 | The system shall allow the tournament's owner to add a team by name.                                                                              | Must     | US-001     |
| FR-002 | The system shall require a team name of 1–60 characters and shall reject a name already used in the same tournament, case-insensitively.           | Must     | US-001     |
| FR-003 | The system shall allow the owner to rename a team, applying the change everywhere that team appears in the schedule.                              | Should   | US-001     |
| FR-004 | The system shall allow the owner to remove a team that appears in no match.                                                                       | Should   | US-001     |
| FR-005 | The system shall refuse to remove a team that appears in at least one match, and shall say which matches block the removal.                       | Must     | US-001     |
| FR-006 | The system shall allow the owner to add a match by selecting a home team and an away team from the tournament's teams, and supplying a kickoff moment and a stage. | Must     | US-002     |
| FR-007 | The system shall reject a match whose home team and away team are the same.                                                                        | Must     | US-002     |
| FR-008 | The system shall support the stages: group stage, round of 16, quarter-final, semi-final, third-place play-off, final.                             | Must     | US-002, US-003 |
| FR-009 | The system shall interpret an entered kickoff moment in the organiser's displayed time zone and store it as an absolute instant.                   | Must     | US-002     |
| FR-010 | The system shall warn, but not block, when a kickoff moment is in the past at the time of entry.                                                   | Should   | US-002     |
| FR-011 | The system shall reject a match that duplicates an existing match in the same tournament with the same home team, away team and stage.             | Must     | US-002     |
| FR-012 | The system shall allow the owner to add a match in any stage at any time, so the schedule may be partial.                                          | Must     | US-003     |
| FR-013 | The system shall allow the owner to edit a match's teams, kickoff moment and stage, subject to FR-007, FR-008 and FR-011.                          | Must     | US-004     |
| FR-014 | The system shall refuse to edit the teams or kickoff moment of a match that has already kicked off.                                                | Must     | US-004     |
| FR-015 | The system shall refuse to delete a match that has at least one prediction or a recorded result, and shall say why.                                | Must     | US-004     |
| FR-016 | The system shall allow the owner to delete a match that has no predictions and no result.                                                         | Should   | US-004     |
| FR-017 | The system shall show the schedule ordered by kickoff moment ascending, grouped by stage, showing both teams and the kickoff in the viewer's time zone. | Must     | US-005     |
| FR-018 | The system shall show the number of matches entered per stage, so the owner can spot an incomplete stage.                                          | Should   | US-005     |
| FR-019 | The system shall restrict every operation in this spec to the tournament's owner.                                                                 | Must     | US-001–US-005 |

## 4. Acceptance Scenarios

### SC-001: Add a team (FR-001)

```gherkin
Given I am signed in as the owner of the tournament "Europees Kampioenschap 2028"
When I add a team named "België"
Then "België" is listed among the tournament's teams
```

### SC-002: Duplicate team name is rejected (FR-002)

```gherkin
Given my tournament already has a team named "België"
When I try to add a team named "belgië"
Then the team is not added
  And I am told that team is already in the tournament
```

### SC-003: Add a group-stage match (FR-006, FR-008, FR-009)

```gherkin
Given my tournament has the teams "België" and "Nederland"
When I add a match "België" versus "Nederland" at 2028-06-14 21:00 in the group stage
Then the match appears in the schedule in the group stage
  And its kickoff is stored as the absolute instant corresponding to 2028-06-14 21:00 in my time zone
```

### SC-004: A team cannot play itself (FR-007)

```gherkin
Given my tournament has the team "België"
When I try to add a match with "België" as both home and away team
Then the match is not added
  And I am told a team cannot play itself
```

### SC-005: Duplicate fixture is rejected (FR-011)

```gherkin
Given my schedule already has "België" versus "Nederland" in the group stage
When I try to add "België" versus "Nederland" in the group stage again
Then the match is not added
  And I am told that fixture already exists in this stage
```

### SC-006: The same two teams may meet again in a different stage (FR-011)

```gherkin
Given my schedule already has "België" versus "Nederland" in the group stage
When I add "België" versus "Nederland" in the semi-final
Then both matches exist in the schedule
```

### SC-007: Add a knockout match once the teams are known (FR-012)

```gherkin
Given my schedule contains only group-stage matches
  And the group stage has finished
When I add a match "België" versus "Portugal" in the quarter-final
Then the match appears in the schedule in the quarter-final
  And the schedule remains valid with no knockout placeholders
```

### SC-008: Correct a wrong kickoff time (FR-013)

```gherkin
Given my schedule has "België" versus "Nederland" kicking off at 2028-06-14 21:00
  And that match has not kicked off yet
When I change its kickoff to 2028-06-14 18:00
Then the schedule shows the match kicking off at 2028-06-14 18:00
```

### SC-009: A started match cannot be edited (FR-014)

```gherkin
Given my schedule has a match whose kickoff is in the past
When I try to change its teams or its kickoff moment
Then the match is unchanged
  And I am told a match that has kicked off can no longer be edited
```

### SC-010: A match with predictions cannot be deleted (FR-015)

```gherkin
Given a player has submitted a prediction for the match "België" versus "Nederland"
When I try to delete that match
Then the match is still in the schedule
  And I am told it cannot be deleted because predictions exist for it
```

### SC-011: A team in use cannot be removed (FR-005)

```gherkin
Given the team "België" appears in two matches
When I try to remove "België" from the tournament
Then the team is still listed
  And I am told which matches must be removed first
```

### SC-012: Review the schedule in kickoff order (FR-017)

```gherkin
Given my schedule has three matches with different kickoff moments across two stages
When I open the schedule
Then the matches are listed by kickoff moment ascending
  And each is shown with its stage, both teams and its kickoff in my time zone
```

### SC-013: Another organiser cannot touch my schedule (FR-019)

```gherkin
Given I am signed in as an organiser who does not own the tournament "Europees Kampioenschap 2028"
When I try to add a match to it
Then no match is added
  And the tournament is reported as not found
```

## 5. Domain Model

### 5.1 Entities

#### Team

A national team or club taking part in the tournament. Teams belong to the tournament they
were entered in; they are not shared across tournaments.

| Attribute    | Type     | Constraints                                               | Description                     |
| ------------ | -------- | --------------------------------------------------------- | ------------------------------- |
| id           | UUID     | PK, generated                                             |                                 |
| tournamentId | UUID     | required, FK → Tournament, immutable                      |                                 |
| name         | string   | required, 1–60 chars, unique per tournament (ci)          | e.g. "België".                   |
| createdAt    | datetime | generated, immutable                                      |                                 |

#### Match

A single fixture: two teams, a kickoff moment and a tournament stage. Kickoff is what locks
predictions (story #007).

| Attribute    | Type     | Constraints                                                        | Description                                  |
| ------------ | -------- | ------------------------------------------------------------------ | -------------------------------------------- |
| id           | UUID     | PK, generated                                                      |                                              |
| tournamentId | UUID     | required, FK → Tournament, immutable                               |                                              |
| homeTeamId   | UUID     | required, FK → Team, must belong to the same tournament             |                                              |
| awayTeamId   | UUID     | required, FK → Team, same tournament, must differ from homeTeamId   |                                              |
| kickoffAt    | datetime | required, stored as an absolute instant (UTC)                      | The moment predictions lock.                  |
| stage        | enum     | required, see Stage value object                                    |                                              |
|              |          | unique on (tournamentId, homeTeamId, awayTeamId, stage)             |                                              |
| createdAt    | datetime | generated, immutable                                                |                                              |

### 5.2 Relationships

- A **Tournament** has many **Teams**; a **Team** belongs to exactly one **Tournament**.
- A **Tournament** schedules many **Matches**; a **Match** belongs to exactly one **Tournament**.
- A **Match** has exactly one home **Team** and exactly one away **Team**, both from its own
  tournament.
- A **Match** is settled by at most one **MatchResult** (story #008) and is the subject of
  many **Predictions** (story #006). Both are out of scope here but constrain FR-015.

### 5.3 Value Objects

#### Stage

The round of the tournament a match belongs to. Ordered, so the schedule can be grouped and
knockout rounds recognised.

| Attribute | Type   | Constraints                                                                             |
| --------- | ------ | ---------------------------------------------------------------------------------------- |
| value     | enum   | one of: `GroupStage`, `RoundOf16`, `QuarterFinal`, `SemiFinal`, `ThirdPlacePlayOff`, `Final` |
| order     | integer | 1..6 in the sequence above, used for grouping and display                               |

### 5.4 Domain Rules and Invariants

- **A team never plays itself**: `homeTeamId ≠ awayTeamId` on every match.
- **Teams stay inside their tournament**: both sides of a match must be teams of that
  match's tournament. A fixture can never mix teams from two tournaments.
- **No duplicate fixture within a stage**: the combination of tournament, home team, away
  team and stage is unique. The same two teams may meet again in a different stage.
- **Kickoff is an absolute instant**: stored in UTC, entered and displayed in a named time
  zone. Comparisons that decide locking (story #007) always use the absolute instant.
- **A started match is frozen**: once `kickoffAt` is in the past, the match's teams and
  kickoff moment are immutable, because predictions have locked against them.
- **Schedules may be incomplete**: a tournament with no knockout matches is valid. Nothing
  in this feature requires a full bracket, and no placeholder fixtures exist.
- **Referenced teams survive**: a team that appears in any match cannot be deleted; renaming
  it is always allowed and propagates everywhere.
- **History is never silently destroyed**: a match carrying predictions or a result cannot
  be deleted.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                     |
| ------- | ------------ | ---------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance  | The schedule view renders in < 400 ms at p95 for a tournament of up to 64 matches and 32 teams.                   |
| NFR-002 | Performance  | Adding a single match completes in < 300 ms at p95.                                                               |
| NFR-003 | Security     | Every read and write in this spec is authorised against the tournament's owner; a non-owner gets "not found".      |
| NFR-004 | Usability    | Adding a match takes at most four inputs (home team, away team, kickoff, stage) and keeps the stage and date of the previous entry pre-filled, because the organiser enters dozens in a row. |
| NFR-005 | Correctness  | Kickoff moments round-trip without drift across time zones and daylight-saving transitions.                       |
| NFR-006 | Scale        | The design targets tournaments of up to 64 matches; nothing may assume fewer than that or require paging below it. |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                             | Expected Behavior                                                                                       |
| ----- | ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------- |
| EC-1  | Organiser adds a match before adding any teams                                        | Offer to add teams first; the match form cannot be submitted with fewer than two teams available.         |
| EC-2  | Kickoff moment is in the past when entered                                            | Warn that predictions for it are already locked, but allow it (FR-010) so a late-started pool can backfill. |
| EC-3  | Kickoff is entered during a daylight-saving transition or as an invalid local time     | Reject with a clear message naming the time zone; do not guess an offset.                                  |
| EC-4  | Two matches are given the exact same kickoff moment                                   | Allowed — simultaneous fixtures are normal. Order them deterministically, by stage then team name.          |
| EC-5  | Organiser edits a match so it duplicates an existing fixture in the same stage         | Reject with the same message as FR-011; the match keeps its previous values.                               |
| EC-6  | Organiser edits a match whose kickoff passes between loading and submitting the form   | Reject on submit (FR-014) and reload the match as it stands.                                               |
| EC-7  | Organiser moves a kickoff from the future into the past                                | Allowed while the match has not yet kicked off; warn that this immediately locks predictions for it.        |
| EC-8  | Organiser renames a team to a name another team in the tournament already has          | Reject as a duplicate (FR-002); the team keeps its name.                                                   |
| EC-9  | Organiser deletes a match while a player is submitting a prediction for it             | The prediction submission fails cleanly with "this match no longer exists"; no orphan prediction is stored. |
| EC-10 | Two browser tabs edit the same match concurrently                                      | Last write wins, but a stale write that violates FR-011 or FR-014 is rejected rather than applied.          |
| EC-11 | Organiser adds a knockout match for teams that were eliminated in the group stage      | Allowed. The system does not model qualification and must not second-guess the organiser.                  |
| EC-12 | Organiser adds more matches to a stage than the stage would normally hold               | Allowed, with no cap. FR-018's per-stage count is informational only.                                      |

## 8. Success Criteria

| ID     | Criterion                                                                                                  |
| ------ | ------------------------------------------------------------------------------------------------------------ |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                                   |
| SC-002 | An organiser can enter a 36-match group stage in under 20 minutes, with the stage and date pre-filled.        |
| SC-003 | No match in the database has the same team on both sides, teams from two tournaments, or a duplicate fixture within its stage. |
| SC-004 | No prediction or result is ever lost through a schedule edit or deletion.                                     |
| SC-005 | Kickoff moments display identically to organiser and players once time zones are accounted for.               |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 001 — Create a pool](001-create-pool.md)**: supplies the `Tournament` and its
  owner. A schedule cannot be entered before a tournament exists.
- **Stories #006 and #007 (predictions and locking)**: consume `Match.kickoffAt` and are the
  reason FR-014 and FR-015 exist.
- **Story #008 (match results)**: adds `MatchResult` against the `Match` entity defined here.

### 9.2 Constraints

- Manual entry is a deliberate decision: EKProno has no football data provider and no
  seeded tournament catalogue. The cost is organiser effort, which runs against the
  "effortless admin" goal in the story map; NFR-004 exists to blunt it, and a bulk import
  is the expected follow-up. Record this as a known debt in
  [docs/architecture/11-risks-and-technical-debt.md](../architecture/11-risks-and-technical-debt.md).
- No placeholder knockout fixtures, so the schedule is incomplete for most of the
  tournament. Any feature that assumes a complete bracket — round recaps (#014), "who
  hasn't predicted the next round" (#010) — must derive rounds from the matches that exist.
- Teams are scoped to a tournament, so team names are retyped for each new tournament.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                             |
| -------------------------------- | ------------------------------------------------------------------------------------- |
| 3. Context & Scope               | Confirms there is no external football data system in the context diagram.             |
| 5. Building Block View           | Introduces the Team and Match building blocks under Tournament.                        |
| 6. Runtime View                  | The add-match and edit-match interactions, including the kicked-off check.             |
| 8. Crosscutting Concepts         | Time zone and instant handling, validation, owner-scoped authorisation.                |
| 9. Architecture Decisions (ADRs) | Manual schedule entry over an external data provider; no placeholder knockout fixtures. |
| 10. Quality Requirements         | Source of the project-wide quality scenarios NFR-001..006 refine.                      |
| 11. Risks and Technical Debt     | Manual-entry effort and the absence of bulk import.                                    |

## 10. Open Questions

| #   | Question                                                                                                              | Owner      | Status | Resolution |
| --- | ---------------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Which time zone does the schedule display in for players — the pool's, or each viewer's own? FR-017 currently says the viewer's. | Christophe | Open   |            |
| 2   | Should a tournament carry a default time zone so the organiser does not pick one per match?                             | Christophe | Open   |            |
| 3   | Is bulk import (paste or CSV) wanted before the first real tournament, given the manual-entry effort noted in §9.2?     | Christophe | Open   |            |

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
