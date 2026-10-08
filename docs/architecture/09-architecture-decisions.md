# 9. Architecture Decisions

<!-- Important, expensive, large-scale, or risky architecture decisions.
     Individual ADRs are stored in docs/architecture/decisions/ -->

## Decision Log

| ID  | Decision | Status | Date |
| --- | -------- | ------ | ---- |
| 001 | Persist behind an `IDataStore` seam, implemented as one JSON document, instead of adopting a database up front | Accepted | 2026-10-08 |
| 002 | Treat authentication as a replaceable stand-in (email only, no password) until a real auth story exists | Accepted | 2026-10-08 |

### ADR-001 — A JSON document behind `IDataStore`

**Context.** [Spec 001](../specs/001-create-pool.md) needs persistence, but EKProno's data
set is tiny (a handful of pools per organiser) and no deployment target has been chosen yet
([07. Deployment View](07-deployment-view.md)).

**Decision.** All persistence goes through `EKProno.Storage.IDataStore`, which exposes a
`Query` over an immutable snapshot and a `Mutate` that commits a private copy only when the
caller accepts the result. The shipped implementation, `JsonFileDataStore`, keeps the whole
data set in memory and mirrors it to `App_Data/ekprono.json`.

**Consequences.**

- The copy-then-commit mutation gives spec 001 NFR-005 (atomic pool creation) and EC-8
  without a transaction manager.
- Tests construct the same store with `filePath: null` for a pure in-memory database, so
  unit and integration tests share the production code path.
- It does not scale beyond a single process, and every write serialises the whole document.
  Recorded in [11. Risks and Technical Debt](11-risks-and-technical-debt.md).
- Replacing it with a real database is one DI registration in `Program.cs`.

### ADR-002 — Authentication is a stand-in

**Context.** Spec 001 §9.1 assumes registration and login already exist; spec 001 open
question 1 notes that no story covers them.

**Decision.** `UserAccountService` resolves an email address to a `UserAccount` and the
`/Account/SignIn` page issues a cookie. There is no password and no verification.

**Consequences.** FR-010 and the organiser-only rules (NFR-003) are enforceable and testable
today, and the whole stand-in is replaceable without touching `PoolService`. It must not
reach production as-is.

See [decisions/](adr/) for full Architecture Decision Records.
