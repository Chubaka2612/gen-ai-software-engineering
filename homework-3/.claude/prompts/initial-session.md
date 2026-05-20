# Initial Session Prompt

Used to start the Claude Code session that produced this specification.

## Prompt

I'm writing a specification-only homework (no code) for a FinTech feature.

Read CLAUDE.md for full context before doing anything.

My chosen domain: Virtual Card Lifecycle — create, freeze/unfreeze,
set spend limits, and view transactions for a regulated EU environment
(PSD2, GDPR, out-of-PCI-CDE by design).

We will produce 4 documents in this order:
1. specification.md — drafted first, everything else derives from it
2. agents.md — drafted after specification.md is approved
3. README.md — drafted last, references the finished spec
4. CLAUDE.md — already exists, no changes needed

Start with specification.md only. Before writing anything, ask me
clarifying questions about scope — which card operations to include,
how strict the compliance layer, assumed tech stack for context,
SLO targets, stakeholder boundaries, and anything else that would
affect the spec structure. Lock these decisions before writing.

When writing specification.md follow this layer structure:
- High-level objective (one crisp statement + scope boundary)
- Mid-level objectives (observable, each gets an ID like OBJ-1)
- Non-functional requirements (measurable — P95 latency in ms, not vague)
- Implementation notes (guardrails for builders)
- Beginning/ending context (what exists before and after work)
- Low-level tasks (many small tasks, each references an OBJ-ID,
  each ends with a Definition of Done checklist)

Cross-cutting requirements to integrate throughout, not in one section:
- Edge cases and failure modes with expected behavior stated explicitly
- Verification per objective (how you know it is met)
- Performance as assumed P95 targets with justification

After specification.md is approved I will ask you to draft agents.md
and then README.md. Do not draft those now.

## Context
- Model: claude-sonnet-4-6
- Date: 2026-05-19
- CLAUDE.md was the only pre-existing file