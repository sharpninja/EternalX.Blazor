# Technical Requirements (MCP Server)

## TR-AI-ASYNC-001

**Non-blocking AI work** — AI and background generation must not block HTTP pipeline.
**Covered by:** FR: FR-AI-002, FR-AI-004; TEST: TEST-AI-001, TEST-AI-003
**Status:** pending
Scope: layer-1+

## TR-AI-GEN-001

**AI selection and generation context** — Server assigns speaker/persona; models never pick figure. Reply context is post plus ancestor chain truncated as needed. AI comments record provider and model. Background selection avoids unbounded continue-from-last-blob growth.
**Covered by:** FR: FR-AI-003, FR-AI-004, FR-AI-006, FR-AI-007, FR-AI-009, FR-AI-013; TEST: TEST-AI-001, TEST-AI-002, TEST-AI-003, TEST-AI-004
**Status:** pending
Scope: layer-1+

## TR-AI-PROV-001

**Multi-provider AiService** — Select provider from DEFAULT_AI_PROVIDER and env keys; server-side only; never expose keys to WASM.
**Covered by:** FR: FR-AI-001, FR-AI-002, FR-AI-003; TEST: TEST-AI-001, TEST-AI-002, TEST-AI-004, TEST-AI-003
**Status:** pending
Scope: layer-1+

## TR-AI-SCAN-001

**Moderator pipeline** — Moderator evaluates NSFW/hate and injection on every user post and AI reply; injection creates ban records.
**Covered by:** FR: FR-AI-001, FR-AI-004, FR-AI-005, FR-AI-006, FR-AI-007, FR-AI-010, FR-AI-011, FR-AI-012; TEST: TEST-AI-001, TEST-AI-002, TEST-AI-004, TEST-AI-003
**Status:** pending
Scope: layer-1+

## TR-AI-SEED-001

**Roster seed mechanics** — Default roster immutable to callers; seed insert-if-absent before unapproved purge. No Reddit community seed required for EternalX.
**Covered by:** FR: FR-AI-001, FR-AI-004, FR-AI-005, FR-AI-007, FR-AI-008, FR-AI-009, FR-CORE-002, FR-CORE-016; TEST: TEST-AI-001, TEST-AI-002, TEST-AI-004, TEST-AI-003, TEST-CORE-001, TEST-CORE-002, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-AI-THREAD-001

**Bounded deep thread generation** — Post-accept generates bounded deep reply thread (target 5-7) through moderation.
**Covered by:** FR: FR-AI-001, FR-AI-003; TEST: TEST-AI-001, TEST-AI-002, TEST-AI-004
**Status:** pending
Scope: layer-1+

## TR-AI-TMR-001

**AutoReplyBackgroundService policy** — Uses AutoReplyPolicy: quiet period, max replies per post, max replies per tick, max context chars.
**Covered by:** FR: FR-AI-002, FR-AI-004, FR-AI-013; TEST: TEST-AI-001, TEST-AI-003
**Status:** pending
Scope: layer-1+

## TR-CORE-API-001

**Client data APIs** — Feed read, post, vote, share, health for Blazor client; no client AI keys.
**Covered by:** FR: FR-CORE-001, FR-CORE-007, FR-UI-002; TEST: TEST-CORE-001, TEST-UI-001
**Status:** pending
Scope: layer-1+

## TR-CORE-CONC-001

**Post write serialization** — Whole-document writes serialized or re-fetch-append-save; AI generation outside lock.
**Covered by:** FR: FR-CORE-003, FR-CORE-006, FR-CORE-009; TEST: TEST-CORE-001, TEST-CORE-002, TEST-CORE-004, TEST-CORE-003
**Status:** pending
Scope: layer-1+

## TR-CORE-DATA-001

**LiteDB persistence conventions** — Single-file LiteDB, UTC times, LITEDB_PATH, posts/replies/users/votes/moderation/figures/peer groups/settings. Seed insert-if-absent.
**Covered by:** FR: FR-CORE-001, FR-CORE-002, FR-CORE-005, FR-CORE-014, FR-CORE-016; TEST: TEST-CORE-001, TEST-AI-001, TEST-CORE-002, TEST-CORE-003, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-DOCKER-001

**Multi-stage container** — Image on 8080, non-root, healthcheck, /app/data volume.
**Covered by:** FR: FR-CORE-014, FR-CORE-015, FR-CORE-021; TEST: TEST-CORE-001, TEST-CORE-004, TEST-CORE-003
**Status:** pending
Scope: layer-1+

## TR-CORE-ENV-001

**Environment-only secrets** — AI provider secrets and GATEWAY_KEY load only from environment / Octopus Sensitive variables. No OIDC client secrets on this site.
**Covered by:** FR: FR-AI-002, FR-CORE-012, FR-CORE-023; TEST: TEST-AI-001, TEST-AI-003, TEST-CORE-003, TEST-AI-004
**Status:** pending
Scope: layer-1+

## TR-CORE-EXPORT-001

**Export bundle contract** — Versioned export/restore for EternalX entities; reject unsupported versions.
**Covered by:** FR: FR-CORE-005, FR-CORE-017; TEST: TEST-CORE-003, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-GW-001

**Gateway auth handler required** — Always register GatewayAuthHandler as the sole authentication scheme. GATEWAY_KEY is required at process start. Build principal only when X-Gateway-Key matches GATEWAY_KEY and X-Auth-UserId is present.
**Covered by:** FR: FR-CORE-003, FR-CORE-005, FR-CORE-010, FR-CORE-011; TEST: TEST-CORE-001, TEST-CORE-002, TEST-CORE-003, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-HLTH-001

**Health endpoint** — /health 200 when process and DB reachable.
**Covered by:** FR: FR-CORE-013, FR-CORE-020; TEST: TEST-CORE-003, TEST-CORE-002
**Status:** pending
Scope: layer-1+

## TR-CORE-LOG-001

**Structured event logging** — Logs for post, AI, moderation, bans, background.
**Covered by:** FR: FR-AI-007, FR-AI-012, FR-CORE-013, FR-CORE-020; TEST: TEST-AI-001, TEST-AI-004, TEST-AI-002, TEST-CORE-003, TEST-CORE-002
**Status:** pending
Scope: layer-1+

## TR-CORE-OCTO-001

**Deploy script CWD contract** — octopus-deploy.ps1 resolves repo root when CWD is script folder; git stderr via cmd /c under Windows PS 5.1.
**Covered by:** FR: FR-CORE-012, FR-CORE-015, FR-CORE-016; TEST: TEST-AI-001, TEST-CORE-003, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-OCTO-002

**Git trigger wiring** — Per-project Git triggers and lifecycle for independent EternalX deploys.
**Covered by:** FR: FR-CORE-012, FR-CORE-013, FR-CORE-015; TEST: TEST-AI-001, TEST-CORE-003
**Status:** pending
Scope: layer-1+

## TR-CORE-OWN-001

**Unique implementation ownership** — No sibling src/tests as compile/copy inputs.
**Covered by:** FR: FR-CORE-001, FR-CORE-018; TEST: TEST-CORE-001
**Status:** pending
Scope: layer-1+

## TR-CORE-PAPI-001

**Post create API** — Authenticated post create with rate limit before side effects; persist LiteDB.
**Covered by:** FR: FR-CORE-003, FR-CORE-006; TEST: TEST-CORE-001, TEST-CORE-002, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-PATH-001

**PathBase** — PATH_BASE=/x support with base href.
**Covered by:** FR: FR-CORE-005, FR-CORE-009, FR-CORE-017, FR-CORE-022; TEST: TEST-CORE-003, TEST-CORE-004, TEST-CORE-002
**Status:** pending
Scope: layer-1+

## TR-CORE-RATE-001

**Sliding window rate limit** — One post per IP per minute.
**Covered by:** FR: FR-CORE-008, FR-CORE-019; TEST: TEST-CORE-001, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-SEC-001

**Gateway identity trust boundary** — Accept X-Auth-* only when GATEWAY_KEY matches X-Gateway-Key; else anonymous.
**Covered by:** FR: FR-CORE-004, FR-CORE-010; TEST: TEST-CORE-001, TEST-CORE-002
**Status:** pending
Scope: layer-1+

## TR-CORE-SEC-002

**Forwarded headers** — Clear KnownIPNetworks/KnownProxies; honor gateway proto/host; PATH_BASE absorption.
**Covered by:** FR: FR-CORE-011, FR-CORE-022; TEST: TEST-CORE-001, TEST-CORE-002
**Status:** pending
Scope: layer-1+

## TR-CORE-SEC-003

**No public site ports in estate mode** — In gateway estate deploys eternalx exposes no public host ports; only gateway is public.
**Covered by:** FR: FR-CORE-009; TEST: TEST-CORE-002, TEST-CORE-003
**Status:** pending
Scope: layer-1+

## TR-CORE-SHARE-001

**Share link generation** — Stable deep links to post/reply.
**Covered by:** FR: FR-CORE-008, FR-CORE-011; TEST: TEST-CORE-001, TEST-CORE-004
**Status:** pending
Scope: layer-1+

## TR-CORE-VOL-001

**Named volume eternalx-data** — Persist under /app/data on eternalx-data volume.
**Covered by:** FR: FR-CORE-015; TEST: TEST-CORE-003
**Status:** pending
Scope: layer-1+

## TR-CORE-VOTE-001

**Vote persistence** — Per-user vote dedupe and net scores; reject anonymous votes.
**Covered by:** FR: FR-CORE-007, FR-CORE-010; TEST: TEST-CORE-001, TEST-UI-001, TEST-CORE-002
**Status:** pending
Scope: layer-1+

## TR-UI-XFEED-001

**X-style feed presentation** — Client layout and components must implement a timeline-centric X/Twitter paradigm for feed, composer, and thread presentation rather than sub-community listing or multi-channel chat shells.
**Covered by:** FR: FR-CORE-007, FR-UI-001, FR-UI-002; TEST: TEST-CORE-001, TEST-UI-001
**Status:** pending
Scope: layer-1+

