# Feature Specification: Invite Friends With a Join Link

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
| Feature ID      | 004                                                            |
| Status          | Draft                                                          |
| Author          | Christophe                                                     |
| Created         | 2026-10-08                                                     |
| Last updated    | 2026-10-08                                                     |
| Epic / Parent   | [Story map — Get everyone in](../product/story-map.md)         |
| Arc42 reference | 3. Context & Scope, 5. Building Block View, 8. Crosscutting Concepts, 9. Decisions |

### 1.1 Problem Statement

A pool with one player in it is not a pool. The organiser needs to get a dozen friends in
without collecting e-mail addresses, maintaining a member list, or chasing anyone through
an account-creation flow they will abandon. The invitation has to be something they can
paste into the group chat and forget about.

### 1.2 Goal

Every pool exposes one shareable join link, built from the secret join token created with
the pool ([spec 001](001-create-pool.md) FR-009). The organiser can see it, copy it, rotate
it if it leaks, and close joining when the friend group is complete. Anyone holding a live
link can join; nobody else can.

### 1.3 Non-Goals

- What happens after someone follows the link — account creation, display-name choice and
  membership are [spec 005](005-join-pool.md).
- Sending the invitation. The system never sends e-mail, SMS or push; the organiser shares
  the link through whatever channel they already use.
- Per-invitee invitations, invite codes with names attached, or tracking who a given link
  was meant for. One pool, one link.
- Approving or rejecting join requests. Holding the link *is* the authorisation.
- Removing a player who has already joined, or transferring the organiser role.

## 2. User Stories

### US-001: Share my pool in one paste

**As an** organiser,
**I want** a single link that lets my friends into my pool,
**so that** I can drop it in the group chat and be done with inviting.

### US-002: Shut the door when everyone is in

**As an** organiser,
**I want** to stop new people joining once my friend group is complete,
**so that** strangers who find the link later cannot get into our pool.

### US-003: Recover from a leaked link

**As an** organiser,
**I want** to replace the join link,
**so that** a link that ended up in the wrong chat stops working without me having to
rebuild the pool.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                             | Priority | User Story     |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------- | -------- | -------------- |
| FR-001 | The system shall derive exactly one join link per pool from the pool's join token.                                                        | Must     | US-001         |
| FR-002 | The system shall show the join link in full to the pool's organiser on the pool's screen.                                                 | Must     | US-001         |
| FR-003 | The system shall offer the organiser a one-action copy of the join link.                                                                  | Must     | US-001         |
| FR-004 | The system shall treat the join link as an absolute URL that works when opened from outside the application.                              | Must     | US-001         |
| FR-005 | The system shall show the join link only to the pool's organiser, never to other players and never to anonymous visitors.                 | Must     | US-001, US-002 |
| FR-006 | The system shall let the organiser close joining for the pool, after which the link no longer admits anyone.                              | Must     | US-002         |
| FR-007 | The system shall let the organiser reopen joining for a closed pool.                                                                      | Must     | US-002         |
| FR-008 | The system shall record whether joining is open or closed, and when it last changed.                                                      | Must     | US-002         |
| FR-009 | The system shall let the organiser rotate the pool's join token, which invalidates the previous link immediately and permanently.          | Must     | US-003         |
| FR-010 | The system shall require the organiser to confirm a rotation, stating that the old link will stop working.                                | Should   | US-003         |
| FR-011 | The system shall leave existing players unaffected by closing, reopening or rotating.                                                     | Must     | US-002, US-003 |
| FR-012 | The system shall generate every join token from a cryptographically secure random source with at least 128 bits of entropy, URL-safe.      | Must     | US-001, US-003 |
| FR-013 | The system shall keep join tokens unique across all pools.                                                                                | Must     | US-001         |
| FR-014 | The system shall show the organiser how many players have joined and when the last one joined.                                            | Should   | US-002         |
| FR-015 | The system shall close joining automatically when the pool starts, i.e. when the earliest kickoff in its schedule is in the past.          | Should   | US-002         |
| FR-016 | The system shall offer the organiser a shareable message containing the pool name and the link, ready to paste.                           | Could    | US-001         |

## 4. Acceptance Scenarios

### SC-001: A new pool has a live join link (FR-001, FR-002)

```gherkin
Given I am the organiser of a pool I just created
When I open the pool's screen
Then I see a join link for the pool
  And joining is open
```

### SC-002: The link is absolute and copyable (FR-003, FR-004)

```gherkin
Given I am the organiser of a pool
When I copy the join link
Then the copied value is an absolute URL containing the pool's join token
  And opening it in a fresh browser reaches the pool's join page
```

### SC-003: Only the organiser sees the link (FR-005)

```gherkin
Given I am a player in a pool but not its organiser
When I open the pool's screen
Then I do not see the join link
  And I have no way to close joining or rotate the link
```

### SC-004: Closing joining turns the link away (FR-006)

```gherkin
Given I am the organiser of a pool with an open join link
When I close joining
Then the pool shows joining as closed
  And someone opening the join link is told the pool is no longer accepting players
```

### SC-005: Reopening lets a latecomer in (FR-007)

```gherkin
Given I am the organiser of a pool whose joining is closed
When I reopen joining
Then the existing join link admits players again
```

### SC-006: Rotation kills the old link (FR-009)

```gherkin
Given I am the organiser of a pool and I have noted its current join link
When I rotate the join link
Then the pool shows a different join link
  And opening the noted link is told the link is no longer valid
  And opening the new link reaches the pool's join page
```

### SC-007: Rotation does not disturb the squad (FR-011)

```gherkin
Given I am the organiser of a pool with five players, including me
When I rotate the join link
Then the pool still has five players
  And their predictions and points are unchanged
```

### SC-008: Rotation is confirmed before it happens (FR-010)

```gherkin
Given I am the organiser of a pool
When I ask to rotate the join link and then cancel the confirmation
Then the join link is unchanged
```

### SC-009: Joining closes itself at first kickoff (FR-015)

```gherkin
Given I am the organiser of a pool whose earliest kickoff has passed
When I open the pool's screen
Then joining is shown as closed because the tournament has started
  And the join link does not admit new players
```

### SC-010: The organiser can see the squad filling up (FR-014)

```gherkin
Given I am the organiser of a pool that three players have joined
When I open the pool's screen
Then I see that the pool has three players
  And I see when the most recent one joined
```

### SC-011: A token is never reused (FR-012, FR-013)

```gherkin
Given a pool whose join token has been rotated twice
When the pool's join tokens so far are compared
Then all three values differ from each other
  And no other pool has ever held any of them
```

## 5. Domain Model

### 5.1 Entities

#### Pool (extended)

Owned by [spec 001](001-create-pool.md); this feature adds the joining state and takes
ownership of the join token's lifecycle.

| Attribute       | Type     | Constraints                                        | Description                                                     |
| --------------- | -------- | -------------------------------------------------- | --------------------------------------------------------------- |
| joinToken       | string   | required, unique, ≥ 128 bits of entropy, URL-safe   | The secret in the join link. Rotatable (FR-009).                 |
| joinState       | enum     | required, [`Open`, `Closed`], default `Open`        | Whether the link currently admits new players.                   |
| joinStateSetAt  | datetime | nullable, set on each change                        | When the organiser last opened or closed joining (FR-008).       |
| tokenRotatedAt  | datetime | nullable, set on each rotation                      | When the join token was last replaced (FR-009).                  |

### 5.2 Relationships

- A **Pool** has exactly one live **JoinToken** at any moment; a token belongs to exactly
  one pool and is never shared or reused.
- Following a join link identifies a **Pool**; it creates no entity by itself —
  [spec 005](005-join-pool.md) creates the **Player**.
- Whether joining is *effectively* open is derived from `joinState` **and** the pool's
  started state (FR-015), the same "started" definition as
  [spec 003](003-scoring-rules.md) FR-008.

### 5.3 Value Objects

#### JoinLink

The absolute URL an organiser shares. Derived, never stored.

| Attribute | Type   | Constraints                                                    |
| --------- | ------ | -------------------------------------------------------------- |
| url       | string | absolute, contains the pool's current `joinToken` as its only secret |

### 5.4 Domain Rules and Invariants

- **One live link per pool**: a pool has exactly one valid join token at a time. Rotation
  replaces it; there is never a grace period in which both work.
- **The token is the credential**: possession of the link is sufficient to reach the join
  page. Nothing else about the pool is disclosed without it.
- **Tokens are opaque**: never derived from the pool name, id, organiser or creation time,
  and never guessable from another pool's token.
- **Tokens are never reused**: a rotated-away token is permanently dead and is never issued
  to any pool again.
- **Joining state never touches membership**: closing, reopening or rotating changes who
  *may* join, never who *has* joined (FR-011).
- **A started pool admits nobody**: once the earliest kickoff has passed, the pool is
  effectively closed regardless of `joinState`, so no one can join after predicting has
  begun.
- **Only the organiser holds the controls**: showing, rotating, closing and reopening are
  organiser-only operations.

## 6. Non-Functional Requirements

| ID      | Category     | Requirement                                                                                                           |
| ------- | ------------ | ---------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Security     | Join tokens are generated from a cryptographically secure random source and are never written to logs, analytics or error messages in full. |
| NFR-002 | Security     | Only the pool's organiser may read the join link or change the joining state; every other request is answered as not found. |
| NFR-003 | Security     | A token has ≥ 128 bits of entropy, making enumeration of live pools infeasible.                                        |
| NFR-004 | Performance  | The pool screen, including link and player count, renders in < 300 ms at p95 for pools up to 50 players.                |
| NFR-005 | Reliability  | Rotation is atomic: either the new token is live and the old one dead, or nothing changed.                              |
| NFR-006 | Usability    | Sharing the pool takes one action from the pool screen — no navigation to a separate invite page.                       |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                 | Expected Behavior                                                                            |
| ----- | ------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------- |
| EC-1  | Someone opens a link whose token matches no pool                          | Report the link as invalid. Do not reveal whether it ever existed or was rotated away.         |
| EC-2  | Someone opens a valid link for a pool whose joining is closed             | Report that the pool is not accepting new players, naming the pool but exposing nothing else.  |
| EC-3  | A player already in the pool opens the join link                          | Send them to the pool — not an error (detailed in [spec 005](005-join-pool.md)).               |
| EC-4  | The organiser rotates while someone is mid-join on the old link           | The old link fails on submission; the would-be player sees the invalid-link message.           |
| EC-5  | Two organiser tabs rotate concurrently                                    | Both rotations succeed in sequence; only the last token is live, and both older ones are dead. |
| EC-6  | Token generation collides with an existing token                          | Retry generation; never issue a duplicate, never fail the request on the first collision.      |
| EC-7  | The organiser closes joining twice                                        | Accept as a no-op; `joinStateSetAt` is not advanced.                                           |
| EC-8  | The organiser reopens joining on a pool that has already started          | Reject: the tournament has started (FR-015). `joinState` may be `Open` but joining stays shut. |
| EC-9  | The link is opened by a signed-out visitor                                | Allowed to reach the join page; authentication is handled by [spec 005](005-join-pool.md).     |
| EC-10 | The link is shared publicly and opened by hundreds of strangers           | All reach the join page; the organiser's remedy is to close joining and rotate.                 |
| EC-11 | A crawler or chat client prefetches the link                              | The join page is safe to fetch: reaching it never creates a player.                             |

## 8. Success Criteria

| ID     | Criterion                                                                                                       |
| ------ | ----------------------------------------------------------------------------------------------------------------- |
| SC-001 | All acceptance scenarios in §4 pass in CI.                                                                        |
| SC-002 | An organiser can invite a friend group with a single copy-and-paste, confirmed on a mobile browser.                |
| SC-003 | No join token appears in any log, trace or error payload during a full invite-and-join run.                       |
| SC-004 | Every pool in the database has exactly one live join token, and no token value occurs twice across all pools ever issued. |
| SC-005 | After rotation, the previous link returns the invalid-link response in 100% of attempts.                          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 001 — Create a pool](001-create-pool.md)**: creates the pool, the first join token
  and the organiser player row.
- **[Spec 005 — Join the pool](005-join-pool.md)**: the consumer of the link; owns everything
  that happens after it is opened.
- **[Spec 002 — Enter the match schedule](002-enter-match-schedule.md)**: supplies the
  kickoff moments behind the automatic close in FR-015.
- A configured public base URL, so the generated link is absolute and correct outside the
  application (FR-004).

### 9.2 Constraints

- The link is the only authorisation to join. There is deliberately no approval step, which
  is acceptable for a friend group and is why rotation and closing exist as the remedy.
- One link per pool, not per invitee. Attributing a join to a specific invitation is not
  possible and is out of scope.
- ASP.NET Core Razor Pages, .NET 10, single project — see
  [docs/architecture/02-architecture-constraints.md](../architecture/02-architecture-constraints.md).

### 9.3 Architecture References

| Arc42 Section                    | Relevance to This Feature                                                       |
| -------------------------------- | --------------------------------------------------------------------------------- |
| 3. Context & Scope               | The link crosses the system boundary into a chat app the system knows nothing about. |
| 5. Building Block View           | Join token lifecycle sits with Pool; the join page is a separate anonymous entry point. |
| 6. Runtime View                  | Share → open → join handover to spec 005; rotation invalidation.                   |
| 8. Crosscutting Concepts         | Secret generation, organiser-scoped authorisation, not-found-instead-of-forbidden. |
| 9. Architecture Decisions (ADRs) | Link-as-credential with no approval step; one link per pool.                        |
| 10. Quality Requirements         | Source of the security scenarios NFR-001..003 refine.                               |
| 12. Glossary                     | Join link, join token, rotation, joining open/closed.                               |

## 10. Open Questions

| #   | Question                                                                                                                         | Owner      | Status | Resolution |
| --- | ---------------------------------------------------------------------------------------------------------------------------------- | ---------- | ------ | ---------- |
| 1   | Should a join link expire on its own after a period of inactivity, or only ever by rotation and the automatic close at kickoff?     | Christophe | Open   |            |
| 2   | Should the organiser be able to cap the number of players a pool accepts?                                                          | Christophe | Open   |            |

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
