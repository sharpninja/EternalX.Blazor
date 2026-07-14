# Byrd Development Process v4 - EternalX.Blazor compliance

**Status:** Mandatory for all work in this repository  
**Canonical process document:** `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`  
**Short names:** Byrd Dev Process, BDP, BPDv4, Byrd Development Process  

This file is the **local compliance contract** for EternalX.Blazor. It does not replace the canonical v4 document. When this file and the canonical document disagree, the canonical v4 document wins.

## Mandate

All planning, implementation, validation, documentation, and deployment work in this repository **must** comply with Byrd Development Process v4. There are no exceptions for size, urgency, or "just a small fix."

Sibling product docs may be read under `docs/` only (`EternalReddit`, `EternalDiscord`). Implementation and tests in those repos are out of scope for this workspace and must not be used as code sources.

## Non-negotiable gates

1. **Requirements first**  
   Capture or update Functional (FR), Technical (TR), and Testing (TEST) requirements through the MCP requirements workflow before implementation. Map FR to TR and TEST. Include requirement IDs on TODOs and in the session log.  
   **Sibling inheritance:** requirements from EternalReddit and EternalDiscord (their `docs/` only) apply here **except** where they conflict with this repo’s requirements; **local wins**. See `docs/requirements-precedence.md`.

2. **Decision-complete plans**  
   Plans are frontier-model handoff artifacts. Lock intended behavior, public interfaces, data shapes, migration/backfill, failure modes, rollout/deployment path, validation commands, and acceptance criteria **before** implementation. Get explicit operator approval before edits that change behavior or architecture.

3. **Tests first (Fowler TDD, Byrd-augmented)**  
   For each small slice of behavior:
   - Write unit tests that express the acceptance criteria for that slice.
   - **Red:** tests fail for the right reason (missing or incorrect behavior).
   - Validate tests with **mocks/stubs** so the tests themselves are proven correct.
   - **Green:** implement the minimum production code that makes those tests pass.
   - **Refactor** tests and production code while staying green.
   - Do not expand scope until the current slice is done.

4. **100 percent green gate (Byrd gate)**  
   Exiting any implementation slice requires the entire executed validation scope (current + prior) to pass: **zero failed tests and zero skipped tests**. Tests are the progress ledger. Do not hide unfinished work with skipped tests. Track deferred scope in MCP TODO/requirements and add its tests when that slice begins.

5. **Mocks-first, then real logic**  
   After tests are written, they must pass against mocks/stubs before real implementation is filled in. This is a Byrd AI-safety augmentation on top of Fowler TDD.

6. **Receipts**  
   Every claim of "done", "fixed", "passes", or "verified" must ship with machine-verifiable evidence (command output and exit codes, on-disk verification, store query results). Summaries may claim only what the cited evidence proves.

7. **MCP process surfaces**  
   Use the required agent plugin for session log, TODO, requirements, and triage. Never edit `docs/todo.yaml` or session-log storage directly. Session turns record interpretation, actions, **design decisions** (conclusions and consequences, not just steps), files modified, and requirement IDs.

8. **No shortcuts around known-correct process**  
   Do not skip red tests, mock validation, full-suite green gates, requirement updates, or session logging because they are inconvenient. Precision outranks convenience.

## Work sequence (each slice)

1. Confirm or create FR/TR/TEST for the slice; select or create MCP TODO.  
2. Write a decision-complete plan (or update an existing approved plan) with named tests and exact validation commands.  
3. Obtain operator approval when the plan is new or the blast radius is non-trivial.  
4. Write failing unit tests for the next small behavior.  
5. Prove tests with mocks (red for missing production behavior; green structure of the tests themselves).  
6. Implement production code until unit tests for the slice are green.  
7. Refactor while green.  
8. Run the full agreed validation scope (current + prior); fix failures; no skips.  
9. Update session log (actions + design decisions), TODOs, and requirements remaining work.  
10. Only then start the next slice or deployment steps.

## Validation and deployment (process level)

- Unit tests gate each implementation slice.  
- Integration tests follow once unit suites are green across the relevant surface, guided by pain points found in implementation.  
- Prefer automated CI build/test before promoting environments.  
- Target at least Development, Staging, and Production when deploying real releases (see canonical v4 Deployment section).  
- This repo's deploy notes live in `docs/deploy/octopus.md`; deployment still requires green validation for the release scope.

## What "compliance" means day to day

- **Bugfix:** reproduce with a failing test first (or a TEST requirement + test), then fix, then full green gate for the affected suite.
- **"Quick" change:** same gates, smaller slice; still red → green → refactor; still requirements touch if behavior changes.
- **Docs-only change:** no TDD cycle required for pure documentation; still log the turn and do not claim code verification without receipts.
- **Planning / design:** requirements and decision-complete plan only; no production implementation until tests for the first slice exist.
- **Ambiguity:** ask the operator; do not invent requirements or skip gates.

## Agent identity for this workspace

- This repository is worked by **GrokCode** under the `mcpserver-grok-plugin` contract.  
- Sibling ownership (reference docs only): EternalReddit → Claude; EternalDiscord → Codex.  
- Session IDs use `GrokCode-<yyyyMMddTHHmmssZ>-<suffix>`.

## References

- Canonical: `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`  
- Product requirements: `docs/REQUIREMENTS.md`  
- Octopus deploy notes: `docs/deploy/octopus.md`  
- MCP marker and plugin rules: `AGENTS-README-FIRST.yaml` (root)
