# Requirements precedence - EternalX.Blazor

**Status:** Mandatory standing rule for this repository  
**Effective:** 2026-07-13  

## Rule

Requirements that apply in the **sibling** product repositories also apply in **this** repository, **except** where they **conflict** with requirements that are native to this repository.

When there is a conflict, **this repository wins**.

## Sibling sources (docs only)

- **EternalReddit** (Claude): `F:\GitHub\EternalReddit\docs\` only. Sibling `src/` and `tests/` are **forbidden** as implementation sources.
- **EternalDiscord** (Codex): `F:\GitHub\EternalDiscord\docs\` only. Sibling `src/` and `tests/` are **forbidden** as implementation sources.

Primary sibling requirement surfaces (when present under `docs/`):

- Hand-written contracts and product docs (e.g. EternalReddit `docs/gateway-sso.md`)
- Exported MCP requirements wiki under `docs/Project/wiki/` (Functional, Technical, Testing, mappings, matrix)
- Any other requirement-bearing markdown under sibling `docs/`

EternalDiscord may grow the same kinds of surfaces later; until then, only its existing `docs/` content applies.

## Local sources (this repo)

Native requirements for EternalX.Blazor include, at minimum:

- `docs/REQUIREMENTS.md` (product specification)
- MCP effective requirements for the **EternalX.Blazor** workspace (FR/TR/TEST via the required plugin)
- `docs/byrd-development-process.md` and process/conduct docs when they constrain behavior
- Accepted plans, session-log design decisions, and TODOs that cite requirement IDs for this workspace

Local sources define the **override set**. Anything in the override set that disagrees with a sibling requirement **displaces** the sibling requirement for work in this repo.

## Conflict resolution

1. **Identify** the sibling requirement (path + quote or ID) and the local requirement (path + quote or ID).
2. **Classify:**
   - **No conflict:** sibling requirement is in force for EternalX.Blazor as well.
   - **Conflict:** local requirement governs; sibling requirement does **not** bind this repo for that point.
3. **Record** the conflict in the active session log as a **design decision** (conclusion + consequence) and, when implementing, on the related MCP TODO / requirement note so the override is auditable.
4. **Do not** silently “merge” conflicting rules into a third undocumented hybrid. Prefer explicit local text that states the intended EternalX behavior.
5. **Do not** pull implementation from sibling `src/` or `tests/` even when a sibling requirement is in force. Satisfy the requirement by designing and implementing **in this repo** under Byrd Dev Process v4.  
   **Ownership:** shared requirements still leave each agent **fully responsible** for a **unique** implementation in their assigned product (GrokCode here). See [`product-architecture.md`](product-architecture.md) section "Implementation ownership".

## Shared core vs divergent UI

Core **data** and **AI interactions** are shared across EternalX, EternalReddit, and EternalDiscord. **UI differs significantly** (Twitter/X, classic Reddit, Discord chat). Architecture: [`product-architecture.md`](product-architecture.md).

Shared-core requirements from sibling docs typically apply here. UI-specific sibling requirements (Reddit listing/sub chrome, Discord channel chrome) do **not** force the same chrome on EternalX; they conflict with local UI paradigm and **local UI wins**.

## Shared vs local (illustrative)

These examples clarify the rule; they are not an exhaustive conflict matrix.

- **Shared / likely in force from siblings:** Eternal network gateway SSO contract (`docs/gateway-sso.md` on EternalReddit) when operating behind the EternalSocial gateway (`X-Gateway-Key`, `X-Auth-*`, path base, forwarded headers). Implement the *contract* here; do not copy sibling handler code from `src/`.
- **Local override (documented conflict):** `docs/REQUIREMENTS.md` §2.1 currently specifies server-side OIDC with Google, Microsoft, and GitHub. That conflicts with the gateway-only auth model. **Local REQUIREMENTS.md wins** for this product until this repo’s requirements are deliberately changed to gateway-first. If both modes are needed, local requirements must be updated to state dual-mode rules explicitly before implementation treats gateway as mandatory.

## Process obligations (BDP v4)

- When planning or implementing, **consult** sibling `docs/` requirement sources **and** local requirements.
- Ingest or map sibling requirements into this workspace’s MCP requirements store when doing substantive product work, tagging provenance (sibling path / export id) and marking superseded items when local overrides apply.
- Every implementation slice still follows Byrd v4: FR/TR/TEST first, decision-complete plan, red tests, mocks-validated, green, full-suite zero-fail zero-skip gate, receipts.

## Related

- Process: `docs/byrd-development-process.md`
- Local product requirements: `docs/REQUIREMENTS.md`
- Agent conduct: `AGENTS.md`
