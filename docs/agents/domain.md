# Domain documentation

This repository uses a **single-context** domain model.

## Layout

- `CONTEXT.md` at the repository root is the shared glossary for gameplay, rules, presentation, and project documentation.
- Architectural Decision Records live under `docs/adr/` when a decision is important, hard to reverse, and not already obvious from the code or glossary.

## Consumer rules

Before specifying, triaging, decomposing, reviewing, or implementing work:

1. Read `CONTEXT.md` and use its domain vocabulary in code, tests, issues, and documentation.
2. Read ADRs that touch the area being changed.
3. Do not silently contradict an accepted ADR. Surface the conflict explicitly if a decision needs to be revisited.
4. Prefer existing domain terms and seams instead of inventing synonyms in one subsystem.
5. Keep `CONTEXT.md` focused on stable domain language. Put architectural trade-offs and hard-to-reverse technical decisions in ADRs instead.

Create `docs/adr/` lazily when the first ADR is needed.
