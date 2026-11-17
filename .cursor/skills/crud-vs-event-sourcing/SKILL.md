---
name: crud-vs-event-sourcing
description: Decide whether a .NET feature/aggregate should be built as CRUD, Event Sourcing, or an event-driven design — and give the concrete implementation path. Use this skill whenever the user describes a domain, feature, aggregate, table, or entity and asks "CRUD or event sourcing?", "should this be event-sourced?", "is event sourcing overkill here?", "do I need events / an event store / a message bus?", mentions audit trail / temporal queries / "who changed what when", or shares a CRUD model and wants to know if it should become event-driven. Always use this skill for architecture-fit questions between CRUD and Event Sourcing instead of answering from memory; it applies a fixed scoring framework and returns a recommendation plus a .NET implementation sketch.
category: Architecture
---

# CRUD vs Event Sourcing

Given a description of a domain, aggregate, or feature, decide the right persistence and architecture style — **CRUD**, **Event Sourcing (ES)**, or **event-driven-but-still-CRUD** — and return a concrete, defensible recommendation with a .NET implementation sketch.

The default answer is CRUD. Event Sourcing must earn its place. This skill exists to stop teams from either (a) reaching for ES because it's fashionable, or (b) staying in CRUD when the domain genuinely needs history and behavior.

## When this runs

The user describes a feature/aggregate ("an Order", "a bank Account", "a user Profile", "a shopping Cart", "a support Ticket") and asks whether it should be CRUD or event-sourced — or shares an existing CRUD model and asks if it should change. If the description is too thin to score, ask **only** for what's missing from the scoring inputs below; otherwise proceed.

Clarify these three separations up front, because they are constantly conflated:

- **CRUD vs Event Sourcing** is a *persistence / state-modeling* choice (store current state vs store the stream of events and fold them into state).
- **Event-driven architecture** is an *integration* choice (components communicate via events/messages). You can be fully event-driven while every service internally uses CRUD.
- **CQRS** (separate read and write models) often pairs with ES but is independent of it. ES almost always implies CQRS; CQRS does not require ES.

Name which of these the user is actually asking about before recommending anything.

## Workflow

1. **Restate the aggregate and its job** in one sentence, so the user can correct a wrong assumption early.
2. **Gather the scoring inputs** (below). If ≥2 are unknown and can't be inferred, ask for those specific ones — nothing else.
3. **Score each of the 8 signals** as favoring CRUD (0), neutral (1), or favoring ES (2). Show the table.
4. **Read the total** against the bands, but override the number when a single hard signal dominates (e.g. a legal, immutable, per-change audit requirement pushes to ES regardless of the sum; a simple lookup/reference table stays CRUD regardless).
5. **Recommend one** of: CRUD, CRUD + domain events for integration, ES for this one aggregate (rest CRUD), or full ES + CQRS. Recommending ES for the *whole system* is almost always wrong — flag it if the user implies that.
6. **Give the .NET implementation sketch** for the recommendation (see reference file).
7. **State the cost** honestly — what ES will make harder (querying, tooling, onboarding, GDPR deletes, schema/versioning of events).

## Scoring inputs (ask only for missing ones)

- **Business invariants / behavior**: is there real domain logic and rules, or is it a form over a table?
- **History requirement**: does the business need *why/when/who* for every change, or only the current value?
- **Audit / compliance**: is an immutable, complete audit log legally or contractually required?
- **Temporal queries**: does anyone need "state as of a past date", replay, or point-in-time reconstruction?
- **Concurrency / collaboration**: multiple actors changing the same aggregate, needing conflict detection?
- **Read/write shape**: same shape for reads and writes, or many different read projections needed?
- **Analytics / derived views**: is value derived from *how state changed over time* (funnels, behavior, trends)?
- **Team & lifecycle**: team familiarity with ES, and expected lifetime/complexity of the system.

## The scoring signals

Score each 0 (CRUD) / 1 (neutral) / 2 (ES).

1. **Domain richness** — CRUD if it's data entry / a lookup table; ES if there's meaningful behavior, state transitions, and invariants (an Order that can be placed/paid/shipped/cancelled, an Account with balance rules).
2. **Value of history** — CRUD if only "current value" matters (a user's display name); ES if every past change is itself valuable (an account ledger, an insurance policy's lifecycle).
3. **Audit is a requirement, not a nice-to-have** — CRUD if a `ModifiedAt`/`ModifiedBy` column is enough; ES (or at least append-only events) if regulators/lawyers need a tamper-evident, complete trail of every intent.
4. **Temporal / "as-of" queries** — CRUD if nobody asks "what did this look like on March 1?"; ES if point-in-time reconstruction, replay, or retroactive correction is real.
5. **Explaining current state** — CRUD if the row tells the whole story; ES if "how did we get here?" is a frequent, hard question (disputes, debugging money, reconciliation).
6. **Read/write divergence** — CRUD if one model serves both; leans ES/CQRS if you need many different read shapes fed from the same write model.
7. **Behavioral analytics** — CRUD if analytics run on current state; ES if the *sequence of events* is the product (recommendations, user-journey analysis, fraud patterns).
8. **Complexity budget** — CRUD if the team is small/unfamiliar, deadline tight, domain simple; ES only if the team can absorb eventual consistency, projections, event versioning, and the extra infra.

### Reading the score

- **0–5 → CRUD.** Don't event-source. If other systems need to react to changes, publish domain events via an **outbox** — that's event-driven, not Event Sourcing.
- **6–10 → CRUD core, selectively event-driven.** Keep CRUD state; add domain events for integration and an audit table for the parts that need it. Consider ES only for the single hottest aggregate if signals 1–5 cluster there.
- **11–16 → Event Sourcing (usually with CQRS) for the aggregate(s) that scored high.** Keep the rest of the system CRUD. Adopt ES per aggregate, never as a blanket rule.

### Hard overrides (beat the number)

- Legal/immutable per-change audit of *intent* required → at least append-only events; ES if behavior is also rich.
- "Undo/replay/retroactive correction" is a core feature → ES.
- Aggregate is a reference/lookup/config table, or a thin CRUD form → CRUD, even if someone is excited about events.
- Right-to-erasure (GDPR) on data that would live inside events → ES is *harder* here (immutable log); call it out and plan crypto-shredding before recommending ES.

## Output format

```
## What you're actually choosing
<one line: CRUD-vs-ES (persistence) vs event-driven (integration) vs CQRS — which one this question is about>

## The aggregate
<one-sentence restatement of the aggregate and its job>

## Score
| Signal | Verdict (CRUD/neutral/ES) | Why |
| ... 8 rows ... |
Total: N/16

## Recommendation
<CRUD | CRUD + outbox events | ES for THIS aggregate | ES + CQRS> — <one paragraph why, incl. any hard override>

## .NET implementation sketch
<the concrete path: libraries, a minimal code shape, projections/outbox as needed — from references/event-sourcing-dotnet.md>

## What this will cost you
<honest downsides of the recommendation: querying, versioning, eventual consistency, GDPR, onboarding>

## If it grows
<the migration trigger + path to the next step, so the decision isn't a dead end>
```

## Principles

- **CRUD is the default and that's fine.** Most tables are correctly CRUD. Recommending ES you don't need is a bug.
- **Never event-source the whole system.** ES is an *aggregate-level* decision. Mixed architectures are normal and correct.
- **Separate the three concepts every time.** People say "event sourcing" when they mean "publish events" or "audit log". Diagnose which they need.
- **Be honest about cost.** Eventual consistency, event versioning/upcasting, harder ad-hoc queries, GDPR deletes, and steeper onboarding are real. State them.
- **Give a migration trigger, not just a verdict.** Tell them the concrete signal that means "now it's time to move from CRUD to events" so the choice stays reversible.
- **Real domain over theory.** Score the aggregate in front of you, not an idealized one.

## Reference

For the .NET specifics — Marten vs EventStoreDB, aggregate/apply patterns, projections, the transactional outbox, event versioning/upcasting, and a CRUD→ES migration path — see `references/event-sourcing-dotnet.md`.

## Pairs well with

- **ddd-aggregate-generator** — once ES is chosen, model the aggregate whose events you'll store.
- **cqrs-mediatr-setup** — the read/write split that ES almost always implies.
- **temporal-tables-setup** — the lightweight "history without ES" option when the score lands in CRUD territory but you still want time-travel on a table.
