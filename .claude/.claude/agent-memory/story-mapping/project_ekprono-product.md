---
name: ekprono-product
description: EKProno is an organiser-first football prediction pool; the product hook is bragging rights (live leaderboard + trash talk), not betting or stats
metadata:
  type: project
---

EKProno is a **football prediction pool**: friends predict match scores for a tournament, earn points, and compete on a leaderboard. The story map (14 stories, 4 activities) lives at `docs/product/story-map.md`.

**Why:** The user's stated hook is **bragging rights** — a live leaderboard and trash talk, "beating my mates." Social/competitive features are first-class product value here, not garnish. The primary user is the **organiser** (creates the pool, invites people, sets scoring rules, enters results), with players as a secondary audience.

**How to apply:**
- When scoping or specc-ing a story, favour what makes the rivalry visible (rank movement, head-to-head, recaps, banter) over analytics, stats depth, or money/betting mechanics — the user never asked for those.
- Admin friction is the enemy: the organiser's flows (schedule loading, result entry, chase-up dashboard) should be as automatic as possible, since "keeps the pool about the rivalry, not the bookkeeping" is the point.
- Activity backbone the user confirmed: Set up the pool → Get everyone in → Keep it running → Drive the rivalry. Keep new stories inside this vocabulary.
- Domain vocabulary established: *pool*, *organiser*, *player*, *prediction*, *scoring rules*, *leaderboard*, *round*.
- Story numbers 001-014 are taken and map to spec prefixes `docs/specs/NNN-<slug>.md`; new stories continue from 015.

Related: [[interview-via-parent]]
