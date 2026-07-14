# Requirement groups: UI, AI, Core

**Status:** Active MCP area grouping for EternalX.Blazor  
**Updated:** 2026-07-13 (import from EternalReddit wiki AI + CORE, adapted)

MCP requirement IDs use first segment as group: `FR-UI-*`, `FR-AI-*`, `FR-CORE-*` (same for TR/TEST).

## Groups

- **UI** - Twitter/X presentation shell and client UX (not imported from Reddit UI)
- **AI** - All AI FRs from EternalReddit wiki (adapted) plus local moderation and auto-reply bounds
- **CORE** - Shared core from EternalReddit wiki (adapted to X timeline / dual auth / eternalx volume) plus local ops/deploy/ownership

## Inventory (verified MCP list)

- **UI FR (2):** FR-UI-001, FR-UI-002  
- **AI FR (13):** FR-AI-001 .. FR-AI-013  
- **CORE FR (23):** FR-CORE-001 .. FR-CORE-023  
- **TR:** 30 (AI + CORE + TR-UI-XFEED-001)  
- **TEST:** 9 (TEST-AI-001..004, TEST-CORE-001..004, TEST-UI-001)  
- **Mappings:** 38 FR mappings created/confirmed  

## Import provenance

- Source docs (read-only): `F:\GitHub\EternalReddit\docs\Project\wiki\github\`  
- Adaptation notes: `docs/Project/imported-ai-core-from-eternalreddit.md`  
- Import script: `docs/Project/scripts/import-ai-core-requirements.ps1`  

## Key EternalX adaptations

- No Reddit multi-sub / devblog / gateway-owner admin as binding product UI  
- Timeline feed instead of communities  
- Gateway-only auth: `GATEWAY_KEY` + proxy `X-Auth-*` (no local OIDC)  
- Title optional on posts  
- Auto-reply quiet gap + hard reply caps (production incident)  
- Unique implementation ownership (no sibling `src/`/`tests/`)  

## Query

- `workflow.requirements.listFr` with `area: AI` or `area: CORE` or `area: UI`
