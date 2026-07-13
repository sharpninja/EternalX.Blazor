# Technical Requirements (MCP Server)

## TR-AI-PROV-001

**Multi-provider AiService** — AiService must select provider from DEFAULT_AI_PROVIDER and environment keys, invoke providers only on the server, and never expose keys to WASM clients.
**Covered by:** FR: FR-AI-002, FR-AI-003; TEST: TEST-AI-001, TEST-AI-002
**Status:** pending
Scope: layer-1+

## TR-AI-THREAD-001

**Deep thread generation** — Post-accept path must generate 5 to 7 moderated AI replies with rotating figures and cross-figure context after a user post.
**Covered by:** FR: FR-AI-001, FR-AI-003; TEST: TEST-AI-002, TEST-MOD-001
**Status:** pending
Scope: layer-1+

## TR-API-MIN-001

**Client data APIs** — Server must expose authenticated/anonymous-appropriate APIs for feed read, post create, vote, share, and health so the Blazor client does not call AI providers directly.
**Covered by:** FR: FR-POST-002, FR-UI-002; TEST: TEST-API-001, TEST-UI-001
**Status:** pending
Scope: layer-1+

## TR-ARCH-OWN-001

**Unique implementation ownership** — EternalX design, code, tests, and validation must be produced in this repository under GrokCode ownership. Shared requirements do not authorize copying sibling source or tests.
**Covered by:** FR: FR-ARCH-001; TEST: TEST-ARCH-001
**Status:** pending
Scope: layer-1+

## TR-AUTH-GW-001

**Gateway auth handler trust boundary** — When GATEWAY_KEY is set, authentication handler must require exact X-Gateway-Key match before mapping X-Auth headers to claims; otherwise treat as anonymous.
**Covered by:** FR: FR-AUTH-002, FR-AUTH-004; TEST: TEST-AUTH-001, TEST-AUTH-002, TEST-NET-001
**Status:** pending
Scope: layer-1+

## TR-AUTH-OIDC-001

**OIDC provider configuration** — Server must configure OpenID Connect for Google, Microsoft, and GitHub using environment-supplied client IDs and secrets and maintain authenticated session cookies for write operations.
**Covered by:** FR: FR-AUTH-001, FR-AUTH-002, FR-AUTH-003; TEST: TEST-AUTH-001
**Status:** pending
Scope: layer-1+

## TR-BG-TMR-001

**AutoReplyBackgroundService** — Background service must tick every 10 seconds, avoid blocking the request pipeline, select quiet active threads, generate one moderated AI reply, and recover after process restart.
**Covered by:** FR: FR-BG-001; TEST: TEST-BG-001
**Status:** pending
Scope: layer-1+

## TR-DATA-CONC-001

**Post write safety** — Post and reply writes must avoid lost updates under concurrent background and user writes, preserving thread integrity.
**Covered by:** FR: FR-DATA-001; TEST: TEST-DATA-001, TEST-DATA-002
**Status:** pending
Scope: layer-1+

## TR-DATA-LITE-001

**LiteDB service conventions** — LiteDbService must use configurable LITEDB_PATH defaulting to /app/data/eternalx.db and persist posts, users, votes, moderation logs, and rate-limit counters for single-instance deployment.
**Covered by:** FR: FR-DATA-001, FR-POST-001; TEST: TEST-DATA-001, TEST-DATA-002, TEST-POST-001
**Status:** pending
Scope: layer-1+

## TR-DEPLOY-DOCKER-001

**Multi-stage container** — Dockerfile must produce a production image on port 8080 with non-root user, /health healthcheck, and documented volume for /app/data; docker-compose must support ngrok sidecar for local OAuth testing.
**Covered by:** FR: FR-DEPLOY-001; TEST: TEST-DEPLOY-001
**Status:** pending
Scope: layer-1+

## TR-DEPLOY-OCTO-001

**Octopus deploy script contract** — deploy/octopus-deploy.ps1 must resolve repo root when Octopus sets CWD to the script folder and avoid treating git stderr progress as terminating errors under Windows PowerShell 5.1.
**Covered by:** FR: FR-DEPLOY-002, FR-DEPLOY-003; TEST: TEST-DEPLOY-001
**Status:** pending
Scope: layer-1+

## TR-DEPLOY-OCTO-002

**Octopus variables and volume** — Octopus project EternalX must map Sensitive AI/OAuth variables, DEFAULT_AI_PROVIDER, LITEDB_PATH, optional GATEWAY_KEY, and mount persistent storage for /app/data across Development, Staging, and Production.
**Covered by:** FR: FR-DEPLOY-002; TEST: TEST-DEPLOY-001
**Status:** pending
Scope: layer-1+

## TR-MOD-SCAN-001

**ModeratorService pipeline** — ModeratorService must evaluate NSFW/hate/violence and prompt-injection on every user post and AI reply before accepting content; injection triggers ban records for user ID and IP.
**Covered by:** FR: FR-AI-001, FR-BG-001, FR-MOD-001, FR-MOD-002, FR-MOD-003; TEST: TEST-AI-002, TEST-MOD-001, TEST-BG-001
**Status:** pending
Scope: layer-1+

## TR-NET-PATH-001

**PathBase and forwarded headers** — When PATH_BASE is set (e.g. /x), the host must UsePathBase and set base href accordingly; ForwardedHeaders must trust gateway X-Forwarded-Proto/Host for public HTTPS URLs.
**Covered by:** FR: FR-AUTH-004, FR-NET-001; TEST: TEST-AUTH-002, TEST-NET-001
**Status:** pending
Scope: layer-1+

## TR-OPS-HLTH-001

**Health endpoint** — /health must return HTTP 200 when the process can serve and LiteDB is reachable; used for Docker HEALTHCHECK and Octopus health steps.
**Covered by:** FR: FR-OPS-001; TEST: TEST-OPS-001
**Status:** pending
Scope: layer-1+

## TR-OPS-LOG-001

**Structured event logging** — Server must emit structured logs for post create, AI provider calls, moderation outcomes, bans, and background replies with fields sufficient for moderation audit.
**Covered by:** FR: FR-MOD-003, FR-OPS-001; TEST: TEST-MOD-001, TEST-OPS-001
**Status:** pending
Scope: layer-1+

## TR-PERF-ASYNC-001

**Non-blocking AI and background work** — AI generation and background reply work must not block the HTTP request pipeline; target first contentful page load under 2s and reply generation under 8s where streaming is available.
**Covered by:** FR: FR-BG-001; TEST: TEST-BG-001
**Status:** pending
Scope: layer-1+

## TR-POST-API-001

**Post creation API** — Server API must accept authenticated post create requests, apply IP rate limiting before business logic, persist posts to LiteDB, and return identifiers suitable for client feed rendering.
**Covered by:** FR: FR-POST-001; TEST: TEST-DATA-001, TEST-POST-001
**Status:** pending
Scope: layer-1+

## TR-POST-RATE-001

**Sliding window rate limit** — Rate limiting must use a sliding window of at most one post per IP per minute, enforced server-side before post creation side effects.
**Covered by:** FR: FR-POST-003; TEST: TEST-POST-001
**Status:** pending
Scope: layer-1+

## TR-SEC-ENV-001

**Environment-only secrets** — Configuration must load AI and OAuth secrets solely from environment variables; repository must not contain live secrets; .env is local-only and gitignored.
**Covered by:** FR: FR-AI-002, FR-SEC-001; TEST: TEST-AI-001
**Status:** pending
Scope: layer-1+

## TR-SOC-SHARE-001

**Share link generation** — Share endpoints must produce stable deep links to post or reply anchors and increment share counts when share is recorded by authenticated users.
**Covered by:** FR: FR-SOC-002; TEST: TEST-SOC-001
**Status:** pending
Scope: layer-1+

## TR-SOC-VOTE-001

**Vote persistence** — Votes must be stored per user and target post/reply with net score aggregation; unauthenticated vote attempts must be rejected.
**Covered by:** FR: FR-SOC-001; TEST: TEST-SOC-001
**Status:** pending
Scope: layer-1+

## TR-UI-XFEED-001

**X-style feed presentation** — Client layout and components must implement a timeline-centric X/Twitter paradigm for feed, composer, and thread presentation rather than sub-community listing or multi-channel chat shells.
**Covered by:** FR: FR-POST-002, FR-UI-001, FR-UI-002; TEST: TEST-API-001, TEST-UI-001
**Status:** pending
Scope: layer-1+

