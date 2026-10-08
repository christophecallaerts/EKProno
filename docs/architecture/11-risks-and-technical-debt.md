# 11. Risks and Technical Debt

## Risks

<!-- Known technical risks, ordered by priority -->

| Risk | Probability | Impact | Mitigation |
| ---- | ----------- | ------ | ---------- |
| The JSON document store ([ADR-001](09-architecture-decisions.md)) holds everything in one process and rewrites the whole file per write. It cannot be scaled out or run behind a load balancer. | High | High | Keep all persistence behind `IDataStore`; swap in a database before the first real tournament. |
| Authentication is a stand-in with no password ([ADR-002](09-architecture-decisions.md)). Anyone may sign in as any email. | Certain | High | Must be replaced before any public deployment — spec 001 open question 1. |
| Two organisers entering the same real-world tournament create two independent `Tournament` records (spec 001 §9.2), so schedules are duplicated by hand. | High | Medium | Accepted while schedules are entered manually; revisit if a shared catalogue becomes worthwhile. |

## Technical Debt

<!-- Known technical debt items -->

| Item | Description | Effort | Priority |
| ---- | ----------- | ------ | -------- |
|      |             |        |          |
