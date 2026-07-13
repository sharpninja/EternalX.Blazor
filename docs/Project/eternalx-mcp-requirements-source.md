# EternalX.Blazor MCP Requirements Source

Provenance: synthesized 2026-07-13 from local `docs/REQUIREMENTS.md`, `docs/product-architecture.md`, `docs/requirements-precedence.md`, `docs/deploy/octopus.md`, and sibling docs under `F:\GitHub\EternalReddit\docs\` (gateway-sso, Project wiki FR/TR/TEST). EternalDiscord `docs/` had no product FR/TR/TEST exports yet. Local wins on auth conflict (standalone multi-provider OIDC is primary; gateway header trust is optional dual-mode). Shared core requirements adapted for EternalX Twitter/X UI; Reddit-only UI/admin/sub features not imported as binding FR for this product. Implementation is unique to GrokCode in this repo.

## Functional Requirements

## FR-ARCH-001: Shared core semantics unique implementation
The EternalX product must implement shared Eternal network core data and AI interaction semantics while remaining a unique implementation owned by GrokCode in this repository. Sibling requirements inform behavior; sibling src and tests must not be used as implementation sources.
Priority: critical | Area: ARCH | Status: pending

## FR-UI-001: Twitter X style user interface
EternalX must present a Twitter/X-style UI (timeline feed, composer, threaded replies, engagement affordances). It must not adopt classic Reddit listing/sub chrome or Discord chat chrome.
Priority: critical | Area: UI | Status: pending

## FR-AUTH-001: Anonymous browse
Users must be able to browse and read all public feed content anonymously without signing in.
Priority: critical | Area: AUTH | Status: pending

## FR-AUTH-002: Authenticated write and engage
Users must be authenticated to create posts, vote, share, or trigger AI reply generation.
Priority: critical | Area: AUTH | Status: pending

## FR-AUTH-003: Standalone multi-provider OIDC
When not operating exclusively behind gateway-enforced SSO, EternalX must support server-side OpenID Connect login with Google, Microsoft (Entra ID), and GitHub. No local username/password accounts. Session must support posting, voting, and sharing. (Local REQUIREMENTS override sibling gateway-only OAuth prohibition for standalone mode.)
Priority: critical | Area: AUTH | Status: pending

## FR-AUTH-004: Optional EternalSocial gateway identity
When GATEWAY_KEY is configured, EternalX must accept identity only when the request X-Gateway-Key matches, building the principal from X-Auth-UserId, X-Auth-Name, and X-Auth-Email. Mismatched or missing key yields anonymous. Spoofed inbound gateway headers without key match must not be trusted. Login/logout links in gateway deployments must target gateway /login and /logout with returnUrl. Path base and forwarded headers must work behind the gateway. (Shared sibling gateway-sso contract, dual-mode with FR-AUTH-003.)
Priority: high | Area: AUTH | Status: pending

## FR-POST-001: Create text posts
Authenticated users must be able to submit new text posts with optional title or topic; posts persist immediately.
Priority: critical | Area: POST | Status: pending

## FR-POST-002: Chronological feed
Posts must appear in a chronological feed newest first.
Priority: high | Area: POST | Status: pending

## FR-POST-003: IP post rate limit
The server must enforce a maximum of one new post per minute per IP address for all users including authenticated users.
Priority: critical | Area: POST | Status: pending

## FR-AI-001: Deep reply thread on user post
When a user posts, the system must automatically generate a deep reply thread of 5 to 7 replies from a rotating set of historical, legendary, or mythical figures.
Priority: critical | Area: AI | Status: pending

## FR-AI-002: Multi-provider server-side AI
The system must support AI providers Hugging Face, Anthropic Claude (default), OpenAI, and xAI Grok using operator-supplied credentials via environment variables. All AI calls must be server-side only; API keys must never be sent to the client.
Priority: critical | Area: AI | Status: pending

## FR-AI-003: In-character crossovers
AI replies must stay in character for the assigned figure and produce unlikely but on-point crossovers between figures.
Priority: high | Area: AI | Status: pending

## FR-MOD-001: Moderate all content
Every new user post and every AI-generated reply must pass through Moderator AI before acceptance.
Priority: critical | Area: MOD | Status: pending

## FR-MOD-002: Prompt injection ban
If prompt injection is detected, the content must be blocked, the user must be auto-banned (IP and user ID recorded), and future posts from that user must be rejected.
Priority: critical | Area: MOD | Status: pending

## FR-MOD-003: NSFW block without ban
NSFW, adult, violent, or hateful content must be blocked without banning the user unless prompt injection is also present. Moderator decisions must be logged.
Priority: high | Area: MOD | Status: pending

## FR-BG-001: Background auto-reply service
A background service on a 10-second timer must scan recently active threads quiet for a few minutes, select interesting AI context, and post one new in-character AI reply, respecting rate limits and moderation (including moderating background replies).
Priority: high | Area: BG | Status: pending

## FR-SOC-001: Upvote and downvote
Authenticated users must be able to upvote (+1) and downvote (-1) posts and replies; counts must be displayed and persisted. Anonymous users may view counts but not vote.
Priority: high | Area: SOC | Status: pending

## FR-SOC-002: Share deep links
Authenticated users must be able to share a post or specific reply via a clean shareable link that opens directly to that item; share counts must be displayed and persisted.
Priority: medium | Area: SOC | Status: pending

## FR-DATA-001: LiteDB persistence
All application data must persist in LiteDB with collections for posts (with embedded replies), users (provider, subject, display name, ban status), votes, moderation logs, and rate-limit counters. The database file must survive container restarts via volume mount at /app/data.
Priority: critical | Area: DATA | Status: pending

## FR-UI-002: Blazor client shell
The Blazor WebAssembly client must provide top nav with logo and login/logout, composer when authenticated, main feed with nested reply threads, action bars (upvote, downvote, share, optional manual AI reply), read-only AI provider status, minimal profile/ban display, share UX, responsive layout, and near-real-time updates without full page refresh (SignalR or polling).
Priority: high | Area: UI | Status: pending

## FR-SEC-001: Secrets and HTTPS
AI credentials and OAuth secrets must come only from environment variables (Sensitive in Octopus). No client-side AI calls. Production must use HTTPS; local dev may use ngrok for HTTPS and OAuth callbacks.
Priority: critical | Area: SEC | Status: pending

## FR-OPS-001: Health and observability
The app must expose /health returning 200 when app and database are healthy. Structured logging must cover post creation, AI calls, moderation decisions, bans, and background replies. Moderation decisions must be queryable for audit.
Priority: high | Area: OPS | Status: pending

## FR-DEPLOY-001: Docker runtime
EternalX must ship as a multi-stage Docker image listening on 8080, non-root, healthcheck on /health, with volume for /app/data. Local docker-compose must include the app and optional ngrok sidecar with .env secrets.
Priority: high | Area: DEPLOY | Status: pending

## FR-DEPLOY-002: Octopus project EternalX
Deployment must support Octopus project EternalX with Deploy Docker Container step, Sensitive variables for AI and OAuth secrets, DEFAULT_AI_PROVIDER, LITEDB_PATH, environment scopes Development/Staging/Production, and persistent volume for LiteDB.
Priority: high | Area: DEPLOY | Status: pending

## FR-DEPLOY-003: Independent per-repo deploy trigger
EternalX must deploy independently via Octopus Git trigger on this repo push to main without requiring a shared multi-repo pipeline. (Shared sibling deploy estate requirement adapted for this product.)
Priority: medium | Area: DEPLOY | Status: pending

## FR-NET-001: Path base behind gateway
When deployed under the EternalSocial gateway path prefix (/x), EternalX must absorb PATH_BASE / Proxy:PathBase with matching document base href and trust X-Forwarded-Proto/Host from the gateway.
Priority: high | Area: NET | Status: pending

## Technical Requirements

## TR-ARCH-OWN-001: Unique implementation ownership
EternalX design, code, tests, and validation must be produced in this repository under GrokCode ownership. Shared requirements do not authorize copying sibling source or tests.
Priority: critical | Area: ARCH | Subarea: OWN | Status: pending

## TR-UI-XFEED-001: X-style feed presentation
Client layout and components must implement a timeline-centric X/Twitter paradigm for feed, composer, and thread presentation rather than sub-community listing or multi-channel chat shells.
Priority: critical | Area: UI | Subarea: XFEED | Status: pending

## TR-AUTH-OIDC-001: OIDC provider configuration
Server must configure OpenID Connect for Google, Microsoft, and GitHub using environment-supplied client IDs and secrets and maintain authenticated session cookies for write operations.
Priority: critical | Area: AUTH | Subarea: OIDC | Status: pending

## TR-AUTH-GW-001: Gateway auth handler trust boundary
When GATEWAY_KEY is set, authentication handler must require exact X-Gateway-Key match before mapping X-Auth-* headers to NameIdentifier, Name, and Email claims; otherwise treat as anonymous. Never accept gateway headers without key match.
Priority: high | Area: AUTH | Subarea: GW | Status: pending

## TR-POST-API-001: Post creation API
Server API must accept authenticated post create requests, apply IP rate limiting before business logic, persist posts to LiteDB, and return identifiers suitable for client feed rendering.
Priority: critical | Area: POST | Subarea: API | Status: pending

## TR-POST-RATE-001: Sliding window rate limit
Rate limiting must use a sliding window of at most one post per IP per minute, enforced server-side before post creation side effects.
Priority: critical | Area: POST | Subarea: RATE | Status: pending

## TR-AI-PROV-001: Multi-provider AiService
AiService must select provider from DEFAULT_AI_PROVIDER and environment keys CLAUDE_API_KEY, OPENAI_API_KEY, GROK_API_KEY, HUGGINGFACE_API_KEY, invoke providers only on the server, and never expose keys to WASM clients.
Priority: critical | Area: AI | Subarea: PROV | Status: pending

## TR-AI-THREAD-001: Deep thread generation
Post-accept path must enqueue or synchronously generate 5 to 7 moderated AI replies with rotating figures and cross-figure context after a user post.
Priority: critical | Area: AI | Subarea: THREAD | Status: pending

## TR-MOD-SCAN-001: ModeratorService pipeline
ModeratorService must evaluate NSFW/hate/violence and prompt-injection patterns on every user post and AI reply before persistence of accepted content; injection triggers ban records for user ID and IP.
Priority: critical | Area: MOD | Subarea: SCAN | Status: pending

## TR-BG-TMR-001: AutoReplyBackgroundService
Background service must tick every 10 seconds, avoid blocking the request pipeline, select quiet active threads, generate one moderated AI reply, and recover after process restart.
Priority: high | Area: BG | Subarea: TMR | Status: pending

## TR-SOC-VOTE-001: Vote persistence
Votes must be stored per user and target post/reply with net score aggregation; unauthenticated vote attempts must be rejected.
Priority: high | Area: SOC | Subarea: VOTE | Status: pending

## TR-SOC-SHARE-001: Share link generation
Share endpoints must produce stable deep links to post or reply anchors and increment share counts when share is recorded by authenticated users.
Priority: medium | Area: SOC | Subarea: SHARE | Status: pending

## TR-DATA-LITE-001: LiteDB service conventions
LiteDbService must use a configurable LITEDB_PATH defaulting to /app/data/eternalx.db, persist posts with embedded replies, users, votes, moderation logs, and rate-limit counters, and remain suitable for single-instance deployment.
Priority: critical | Area: DATA | Subarea: LITE | Status: pending

## TR-DATA-CONC-001: Post write safety
Post and reply writes must avoid lost updates under concurrent background and user writes (serialization or re-fetch-append-save patterns), preserving thread integrity.
Priority: high | Area: DATA | Subarea: CONC | Status: pending

## TR-SEC-ENV-001: Environment-only secrets
Configuration binding must load AI and OAuth secrets solely from environment variables; repository must not contain live secrets. .env is local-only and gitignored.
Priority: critical | Area: SEC | Subarea: ENV | Status: pending

## TR-OPS-HLTH-001: Health endpoint
/health must return HTTP 200 when the process can serve and LiteDB is reachable; use for Docker HEALTHCHECK and Octopus health steps.
Priority: high | Area: OPS | Subarea: HLTH | Status: pending

## TR-OPS-LOG-001: Structured event logging
Server must emit structured logs for post create, AI provider calls, moderation outcomes, bans, and background replies with enough fields for audit of moderation decisions.
Priority: high | Area: OPS | Subarea: LOG | Status: pending

## TR-PERF-ASYNC-001: Non-blocking AI and background work
AI generation and background reply work must not block the HTTP request pipeline; target first contentful page load under 2s and reply generation under 8s where streaming is available.
Priority: medium | Area: PERF | Subarea: ASYNC | Status: pending

## TR-DEPLOY-DOCKER-001: Multi-stage container
Dockerfile must produce a production image on port 8080 with non-root user, /health healthcheck, and documented volume for /app/data. docker-compose must support ngrok sidecar for local OAuth testing.
Priority: high | Area: DEPLOY | Subarea: DOCKER | Status: pending

## TR-DEPLOY-OCTO-001: Octopus Docker deploy script contract
deploy/octopus-deploy.ps1 must resolve repo root when Octopus sets CWD to the script folder (probe parent of PSScriptRoot), support git-sourced steps under Windows PowerShell 5.1, and avoid treating git stderr progress as terminating errors.
Priority: high | Area: DEPLOY | Subarea: OCTO | Status: pending

## TR-DEPLOY-OCTO-002: Octopus variables and volume
Octopus project EternalX must map Sensitive AI/OAuth variables, DEFAULT_AI_PROVIDER, LITEDB_PATH, optional GATEWAY_KEY, and mount persistent storage for /app/data across Development, Staging, and Production scopes.
Priority: high | Area: DEPLOY | Subarea: OCTO | Status: pending

## TR-NET-PATH-001: PathBase and forwarded headers
When PATH_BASE or Proxy:PathBase is set (e.g. /x), the host must UsePathBase and set base href accordingly; ForwardedHeaders must trust gateway X-Forwarded-Proto/Host so redirects and links use the public HTTPS origin.
Priority: high | Area: NET | Subarea: PATH | Status: pending

## TR-API-MIN-001: Client data APIs
Server must expose authenticated/anonymous-appropriate minimal APIs or controllers for feed read, post create, vote, share, and health so the Blazor client does not call AI providers directly.
Priority: high | Area: API | Subarea: MIN | Status: pending

## Test Requirements

## TEST-ARCH-001: Unique implementation policy tests
Automated checks or review gate must fail if build scripts or code references sibling repo src/tests paths as compile or copy inputs for EternalX.
Priority: medium | Area: ARCH | Status: pending

## TEST-UI-001: X-style shell component tests
Unit tests for Blazor components must cover feed list ordering, composer visibility when authenticated vs anonymous, and thread nesting presentation for the X-style shell.
Priority: high | Area: UI | Status: pending

## TEST-AUTH-001: OIDC and anonymous access
Tests must verify anonymous read is allowed, write endpoints return 401 when unauthenticated, and OIDC configuration registers Google, Microsoft, and GitHub schemes.
Priority: critical | Area: AUTH | Status: pending

## TEST-AUTH-002: Gateway header trust
When GATEWAY_KEY is configured, tests must verify principal is established only with matching X-Gateway-Key plus X-Auth headers, and that mismatched key yields anonymous regardless of X-Auth headers.
Priority: high | Area: AUTH | Status: pending

## TEST-POST-001: Post create and rate limit
Tests must verify authenticated post create persists content and that a second post from the same IP within one minute is rejected.
Priority: critical | Area: POST | Status: pending

## TEST-AI-001: Server-side AI isolation
Tests must verify AI service is not registered for client-side invocation and that provider selection respects DEFAULT_AI_PROVIDER with mocked providers.
Priority: critical | Area: AI | Status: pending

## TEST-AI-002: Deep thread generation
Tests with mocked AI must verify 5 to 7 reply generation path is invoked after accepted user post and each reply is submitted through moderation.
Priority: high | Area: AI | Status: pending

## TEST-MOD-001: Injection ban and NSFW block
Tests must verify prompt injection content is rejected and creates ban state, NSFW-only content is rejected without ban, and clean content is accepted.
Priority: critical | Area: MOD | Status: pending

## TEST-BG-001: Background service tick
Tests must verify AutoReplyBackgroundService selects a candidate thread and posts one moderated reply using mocks without blocking a sample HTTP request.
Priority: high | Area: BG | Status: pending

## TEST-SOC-001: Vote and share authorization
Tests must verify anonymous vote/share rejected, authenticated upvote/downvote updates scores, and share link generation returns deep link targets.
Priority: high | Area: SOC | Status: pending

## TEST-DATA-001: LiteDB persistence round-trip
Tests must verify posts, replies, users, votes, and moderation logs round-trip through LiteDbService and survive reopen of the database file.
Priority: critical | Area: DATA | Status: pending

## TEST-DATA-002: Concurrent reply durability
Concurrency tests must verify interleaved user and background replies do not drop posts or lose reply threads.
Priority: high | Area: DATA | Status: pending

## TEST-OPS-001: Health endpoint
Tests must verify /health returns 200 when dependencies are healthy and fails when database is unavailable if that failure mode is detectable.
Priority: high | Area: OPS | Status: pending

## TEST-DEPLOY-001: Deploy script and docker contract
Tests or CI checks must validate octopus-deploy.ps1 parses under PowerShell, resolves repo root from script-folder CWD, and Dockerfile exposes 8080 with healthcheck metadata.
Priority: medium | Area: DEPLOY | Status: pending

## TEST-NET-001: PathBase and forwarded headers
Tests must verify path base stripping/absorption and that forwarded proto/host influence generated public URLs when configured.
Priority: high | Area: NET | Status: pending

## TEST-API-001: Feed and write API surface
Integration tests must verify anonymous feed read, authenticated post, and rejection of client attempts to pass AI API keys.
Priority: high | Area: API | Status: pending
