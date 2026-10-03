# EventTix Hub

Spec, decisions, conventions and tickets T01–T20 live in `PLAN.md`. Read it before implementing. Conventions in §3 are the code-review standards.

## Agent skills

### Issue tracker

Issues and specs are local markdown files under `.scratch/<feature>/`. `PLAN.md` is the working spec and ticket list. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-role vocabulary (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` + `docs/adr/` at the repo root; until `GLOSSARY.md` exists, `PLAN.md` §4–§6 stand in. See `docs/agents/domain.md`.
