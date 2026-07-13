# EternalX.Blazor - agent conduct

## Process (mandatory)

**All work in this repository must comply with Byrd Development Process v4.**

- Local contract: [`docs/byrd-development-process.md`](docs/byrd-development-process.md)
- Canonical document: `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`

Non-negotiable: requirements first (FR/TR/TEST via MCP), decision-complete plans, tests first (red → mocks-validated → green → refactor), 100% green gates (no failed, no skipped tests in the executed scope), receipts for every completion claim, MCP plugin for session/TODO/requirements (never edit `docs/todo.yaml` or session storage directly).

## Sibling repos

- `F:\GitHub\EternalReddit` (Claude): read `docs/` only; never `src/` or `tests/`
- `F:\GitHub\EternalDiscord` (Codex): read `docs/` only; never `src/` or `tests/`

## Product architecture

**Shared core data and AI interactions** across EternalX, EternalReddit, and EternalDiscord. **UIs differ:**

- **EternalX (this repo):** Twitter / X-style UI  
- **EternalReddit:** classic Reddit UI  
- **EternalDiscord:** Discord chat UI  

Details: [`docs/product-architecture.md`](docs/product-architecture.md). Implement shared semantics here under BDP v4; do not import sibling UI chrome or sibling `src/`/`tests/`.

## Implementation ownership

**Shared requirements do not mean shared implementation.** Each agent is **fully responsible** for a **unique** implementation in their own repo:

- **GrokCode** → EternalX.Blazor (this workspace)  
- **Claude** → EternalReddit  
- **Codex** → EternalDiscord  

GrokCode designs, builds, tests, and validates EternalX here. Do not copy sibling code or tests; do not treat sibling repos as implementation libraries.

## Requirements precedence

**Sibling requirements apply here unless they conflict with this repo’s requirements; local wins on conflict.**

Full rule: [`docs/requirements-precedence.md`](docs/requirements-precedence.md).

When planning or implementing, consult sibling `docs/` (including MCP wiki exports under `docs/Project/wiki/` when present) **and** local `docs/REQUIREMENTS.md` / MCP effective requirements for EternalX.Blazor. Log conflicts as design decisions; do not invent silent hybrids.

## Identity

- Agent / sourceType for this workspace: **GrokCode**
- Marker and MCP bootstrap: `AGENTS-README-FIRST.yaml`
