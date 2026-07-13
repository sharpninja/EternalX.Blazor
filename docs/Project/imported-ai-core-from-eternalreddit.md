# Imported AI + CORE requirements (EternalReddit wiki → EternalX)

**Source:** `F:\GitHub\EternalReddit\docs\Project\wiki\github\` (docs only)  
**Target:** EternalX.Blazor MCP (areas AI and CORE only; UI not imported)  
**Adaptation rules:** Twitter/X feed (no Reddit subs/devblog chrome); **gateway-only auth** (`GATEWAY_KEY` + `X-Auth-*`, no local OIDC); unique implementation in this repo; AutoReply quiet-gap + reply caps from production fix.

---

## AI functional (FR-AI-*)

## FR-AI-001: Per-provider model and effort selection
Admins (or operator config) set the AI model and reasoning effort per provider for the EternalX feed via configuration or admin surface populated from live provider catalogs, with fallback to provider env defaults when unset. Reply metadata displays the provider and model in use.
Priority: high | Area: AI | Status: pending
Notes: Imported from EternalReddit FR-AI-001; adapted from per-sub to feed-level (X has no Reddit subs). Source wiki github Functional-Requirements.md

## FR-AI-002: AI feed control
Operators can pause and resume auto-reply (and any auto-post) background services, seed content on demand, and view basic stats. Auto-replies respect a quiet gap after the last message (default minutes-scale quiet period plus tick interval) and must not unbounded-loop. If the feed is idle for about an hour, a figure may create an original post.
Priority: high | Area: AI | Status: pending
Notes: From Reddit FR-AI-002; quiet-gap/cap aligned with AutoReplyPolicy fix (MaxRepliesPerPost, MinQuietPeriod).

## FR-AI-003: User post submission with AI replies
Authenticated users can submit posts (text required; title optional for X). AI figures reply in-character on a realistic cadence. Reply generation may round-robin across configured providers (Claude, Grok, OpenAI, HuggingFace). Deep initial threads remain bounded (target 5-7 replies).
Priority: critical | Area: AI | Status: pending
Notes: From Reddit FR-AI-003; title optional per local REQUIREMENTS.md (Reddit title mandatory overridden).

## FR-AI-004: Content boundaries and roster curation
Figures whose primary legacy is inseparable from atrocity, violent conquest, or oppression are excluded from the roster. Satire stays affectionate and clearly hypothetical - no fabricated quotes presented as real historical record. Replies may only come from the approved roster. Documented scripted gags (if any) are explicit exceptions.
Priority: high | Area: AI | Status: pending
Notes: From Reddit FR-AI-004; EternalX implements its own roster seed uniquely.

## FR-AI-005: Curated historical cast
The cast is a curated roster of approved historical, legendary, and mythical figures. Each figure has a name and a rich persona defining voice, temperament, quirks, and biographical beats. Count and membership are data-driven (not hardcoded forever).
Priority: high | Area: AI | Status: pending
Notes: From Reddit FR-AI-005; roster size may match or diverge as data, not as a hard code constant.

## FR-AI-006: In-character generation
The server assigns the figure and persona for every AI post and reply - models never choose who they are. Replies stay in-character, ground humor in real biographical material, keep tone playful and affectionate, and favor crossovers and interactions between historically related peers.
Priority: critical | Area: AI | Status: pending
Notes: From Reddit FR-AI-006; matches local crossover intent.

## FR-AI-007: Peer-group membership for figure picks
Peer groups (e.g. composers, scientists, writers, philosophers, generals, leaders, myth, stage-screen) organize the cast. Figures may belong to several groups. AI picks draw from allowed groups for the active feed context with fallback so the feed never dead-ends.
Priority: medium | Area: AI | Status: pending
Notes: From Reddit FR-AI-007; adapted without Reddit community/sub membership modes.

## FR-AI-008: Figure lifecycle
A figure existing in the roster means approved - content from unapproved names is purged. An Enabled flag can bench a figure from new AI picks without deleting history. Operators manage figures and group assignments from admin/config surfaces.
Priority: medium | Area: AI | Status: pending
Notes: From Reddit FR-AI-008.

## FR-AI-009: Scripted gag exception (optional Columbus pattern)
Optional scripted non-model gags (e.g. Columbus "First!") if enabled for EternalX must be documented exceptions: exempt from normal model generation and from unapproved-name purge, never presented as model output.
Priority: low | Area: AI | Status: pending
Notes: From Reddit FR-AI-009; optional for X - implement only if product chooses the gag.

## FR-AI-010: Moderate all content
Every new user post and every AI-generated reply must pass through moderation before acceptance (NSFW/hate and prompt-injection checks).
Priority: critical | Area: AI | Status: pending
Notes: Local EternalX requirement retained; not in Reddit AI list as separate FR.

## FR-AI-011: Prompt injection ban
If prompt injection is detected, content is blocked, the user is auto-banned (IP + user id), and future posts from that user are rejected.
Priority: critical | Area: AI | Status: pending
Notes: Local EternalX.

## FR-AI-012: NSFW block without ban
NSFW/adult/violent/hateful content is blocked without ban unless injection is also present. Decisions are logged.
Priority: high | Area: AI | Status: pending
Notes: Local EternalX.

## FR-AI-013: Background auto-reply bounds
Background auto-reply must enforce MaxRepliesPerPost, MinQuietPeriod, MaxRepliesPerTick, and MaxContextChars (or equivalent) so threads cannot unbounded-grow as observed in production (565-reply chain).
Priority: critical | Area: AI | Status: pending
Notes: From EternalX production incident + AutoReplyPolicy.

---

## CORE functional (FR-CORE-*)

## FR-CORE-001: Timeline feed (not multi-sub communities)
EternalX presents a single chronological (or ranking-capable) timeline feed of posts and threaded replies for the X-style product. Community/sub Reddit structures are out of scope for this product UI; optional future topics must not imply Reddit subs.
Priority: critical | Area: CORE | Status: pending
Notes: Adapted from Reddit FR-CORE-001 (multi-subs) → X timeline.

## FR-CORE-002: Peer groups for figures
Historical figures belong to zero or more peer groups used for AI casting and discovery. Empty allowlists mean open to all approved enabled figures with safe fallback.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-002; without sub membership modes.

## FR-CORE-003: Authenticated human posts and replies
Authenticated users can post and reply in threads. Human authors carry identity, render without AI provider badges, and survive restarts (exempt from unapproved AI purge).
Priority: critical | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-003; X thread model.

## FR-CORE-004: Owner-gated admin surface
Admin UI and admin APIs are restricted to the owner account (Authorization__AdminEmail / configured owner). Enforcement is server-side.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-004.

## FR-CORE-005: Admin data tools
Admin surface provides export, restore, and clear-feed (or equivalent) operations for EternalX data.
Priority: medium | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-005.

## FR-CORE-006: No lost updates under concurrency
Concurrent user and background writes must not drop posts or reply threads (serialization or re-fetch-append-save patterns).
Priority: critical | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-007 (renumbered for EternalX CORE list).

## FR-CORE-007: Voting
Authenticated users get at most one vote per post or reply (deduplicated). Vote scores are displayed and persisted. Anonymous users can read scores only.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-008; X engagement model.

## FR-CORE-008: Share deep links
Authenticated users can share a post or reply via a clean deep link; share counts may be tracked.
Priority: medium | Area: CORE | Status: pending
Notes: Local EternalX social share requirement.

## FR-CORE-009: Gateway estate participation
EternalX participates in the EternalSocial gateway estate at path /x on the shared docker network. Gateway landing/admin live in the gateway product; EternalX must operate correctly as a proxied site.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-009; site role not gateway-owner role.

## FR-CORE-010: Dual-mode authentication
When GATEWAY_KEY is configured, identity is accepted only with matching X-Gateway-Key and X-Auth-* headers (anonymous otherwise). When not in gateway mode, EternalX supports standalone OIDC (Google, Microsoft, GitHub). Local dual-mode overrides pure gateway-only sibling wording.
Priority: critical | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-010 + local AUTH precedence override.

## FR-CORE-011: Persistent sign-in
Logged-in sessions survive visits within cookie lifetime (gateway long-lived cookie in gateway mode; cookie auth in standalone mode).
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-012.

## FR-CORE-012: Per-repo deploy triggers
EternalX deploys independently via Octopus Git trigger on this repo push to main; no multi-repo shared pipeline required.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-013.

## FR-CORE-013: Stable shared GATEWAY_KEY
GATEWAY_KEY is a stable sensitive library variable so EternalX can restart independently without breaking SSO with the gateway.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-014.

## FR-CORE-014: Data-driven content and config
Posts/replies, figures, peer groups, users, votes, moderation logs, and settings live in LiteDB. Prefer data-driven roster and settings over hardcoding.
Priority: critical | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-015; X entities not Reddit communities.

## FR-CORE-015: Durable volume persistence
All durable content lives under /app/data on named volume eternalx-data (LITEDB_PATH). Survives container replace.
Priority: critical | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-016; volume name eternalx-data.

## FR-CORE-016: Idempotent seeding
First run seeds default figures and peer groups (and any X-specific defaults). Re-seed is insert-if-absent and does not clobber operator edits.
Priority: high | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-017; no 12 Reddit communities seed.

## FR-CORE-017: Versioned export and restore
Admin export produces a versioned JSON snapshot. Restore rejects unsupported versions; clear-feed removes posts without wiping roster/config when that operation exists.
Priority: medium | Area: CORE | Status: pending
Notes: From Reddit FR-CORE-018.

## FR-CORE-018: Unique implementation ownership
EternalX design, code, tests, and validation are owned by GrokCode in this repository. Shared requirements do not authorize copying sibling src/tests.
Priority: critical | Area: CORE | Status: pending
Notes: Local architecture rule.

## FR-CORE-019: IP post rate limit
Maximum one new post per minute per IP, enforced server-side before side effects.
Priority: critical | Area: CORE | Status: pending
Notes: Local REQUIREMENTS.md.

## FR-CORE-020: Health and observability
/health returns 200 when app and DB are healthy. Structured logs for posts, AI, moderation, bans, background replies.
Priority: high | Area: CORE | Status: pending
Notes: Local.

## FR-CORE-021: Docker runtime
Multi-stage image on 8080, non-root, healthcheck, volume for /app/data; compose supports local ngrok for OAuth testing when standalone.
Priority: high | Area: CORE | Status: pending
Notes: Local + deploy docs.

## FR-CORE-022: Path base and forwarded headers
When PATH_BASE=/x, UsePathBase + base href; trust gateway X-Forwarded-Proto/Host.
Priority: high | Area: CORE | Status: pending
Notes: From gateway-sso + Reddit TR-CORE-SEC-002 site obligations.

## FR-CORE-023: Secrets from environment only
AI and OAuth secrets only via environment / Octopus Sensitive variables; never client-side AI keys.
Priority: critical | Area: CORE | Status: pending
Notes: Local.

---

## AI technical

## TR-AI-GEN-001: AI selection and generation context
Server assigns speaker/persona; models never pick their own figure. Reply context is the post plus ancestor chain (truncated as needed). Every AI comment records provider and model. Thread selection for background work prefers recent active threads without unbounded continue-from-last-blob growth.
Priority: critical | Area: AI | Subarea: GEN | Status: pending
Notes: From Reddit TR-AI-GEN-001; truncation/cap from EternalX incident.

## TR-AI-SEED-001: Roster seed mechanics
Default roster collections are immutable to callers; seed inserts per-id if absent and runs before unapproved purge. EternalX seed does not require Reddit community rows.
Priority: high | Area: AI | Subarea: SEED | Status: pending
Notes: From Reddit TR-AI-SEED-001 adapted.

## TR-AI-PROV-001: Multi-provider AiService
AiService selects provider from DEFAULT_AI_PROVIDER and env keys; server-side only; never expose keys to WASM.
Priority: critical | Area: AI | Subarea: PROV | Status: pending

## TR-AI-THREAD-001: Bounded deep thread generation
Post-accept path generates a bounded deep reply thread (target 5-7) through moderation.
Priority: critical | Area: AI | Subarea: THREAD | Status: pending

## TR-AI-SCAN-001: Moderator pipeline
Moderator evaluates NSFW/hate and injection on every user post and AI reply; injection creates ban records.
Priority: critical | Area: AI | Subarea: SCAN | Status: pending

## TR-AI-TMR-001: AutoReplyBackgroundService policy
Background service uses AutoReplyPolicy: quiet period, max replies per post, max replies per tick, max context chars; logs failures without silent swallow where possible.
Priority: critical | Area: AI | Subarea: TMR | Status: pending

## TR-AI-ASYNC-001: Non-blocking AI work
AI and background generation must not block the HTTP pipeline; target reply generation under 8s when streaming available.
Priority: medium | Area: AI | Subarea: ASYNC | Status: pending

## CORE technical

## TR-CORE-DATA-001: LiteDB persistence conventions
Single-file LiteDB with UTC times, durable path from LITEDB_PATH, posts with embedded replies, users, votes, moderation logs, rate-limit counters, figures/peer groups/settings as needed. Seed insert-if-absent.
Priority: critical | Area: CORE | Subarea: DATA | Status: pending
Notes: From Reddit TR-CORE-DATA-001 adapted to X entities.

## TR-CORE-CONC-001: Post write serialization
Whole-document writes serialized or re-fetch-append-save for replies; AI generation outside lock.
Priority: high | Area: CORE | Subarea: CONC | Status: pending
Notes: From Reddit TR-CORE-CONC-001.

## TR-CORE-SEC-001: Gateway identity trust boundary
Sites accept X-Auth-* only when GATEWAY_KEY matches X-Gateway-Key; otherwise anonymous. Trust boundary docker network + shared key.
Priority: high | Area: CORE | Subarea: SEC | Status: pending
Notes: From Reddit TR-CORE-SEC-001.

## TR-CORE-SEC-002: Forwarded headers
ForwardedHeaders Clear KnownIPNetworks/KnownProxies; honor gateway proto/host; PATH_BASE absorption.
Priority: high | Area: CORE | Subarea: SEC | Status: pending
Notes: From Reddit TR-CORE-SEC-002.

## TR-CORE-SEC-003: No public site ports in estate mode
In gateway estate deploys, eternalx exposes no public host ports; only gateway is public.
Priority: high | Area: CORE | Subarea: SEC | Status: pending
Notes: From Reddit TR-CORE-SEC-003.

## TR-CORE-VOL-001: Named volume eternalx-data
Persist under /app/data on eternalx-data volume.
Priority: high | Area: CORE | Subarea: VOL | Status: pending
Notes: From Reddit TR-CORE-VOL-001.

## TR-CORE-OCTO-001: Deploy script CWD contract
octopus-deploy.ps1 resolves repo root when CWD is script folder; git stderr via cmd /c under Windows PS 5.1.
Priority: high | Area: CORE | Subarea: OCTO | Status: pending
Notes: From Reddit TR-CORE-OCTO-001.

## TR-CORE-OCTO-002: Git trigger wiring
Per-project Git triggers and lifecycle for independent EternalX deploys.
Priority: medium | Area: CORE | Subarea: OCTO | Status: pending
Notes: From Reddit TR-CORE-OCTO-002.

## TR-CORE-OIDC-001: Standalone multi-provider OIDC
Configure Google, Microsoft, GitHub OIDC from env when not gateway-only.
Priority: critical | Area: CORE | Subarea: OIDC | Status: pending
Notes: Local override path.

## TR-CORE-GW-001: Gateway auth handler
When GATEWAY_KEY set, map headers only on key match.
Priority: high | Area: CORE | Subarea: GW | Status: pending

## TR-CORE-PAPI-001: Post create API
Authenticated post create with rate limit before side effects; persist LiteDB.
Priority: critical | Area: CORE | Subarea: PAPI | Status: pending

## TR-CORE-RATE-001: Sliding window rate limit
One post per IP per minute.
Priority: critical | Area: CORE | Subarea: RATE | Status: pending

## TR-CORE-VOTE-001: Vote persistence
Per-user vote dedupe and net scores; reject anonymous votes.
Priority: high | Area: CORE | Subarea: VOTE | Status: pending

## TR-CORE-SHARE-001: Share link generation
Stable deep links to post/reply.
Priority: medium | Area: CORE | Subarea: SHARE | Status: pending

## TR-CORE-ENV-001: Environment-only secrets
AI/OAuth secrets only from environment.
Priority: critical | Area: CORE | Subarea: ENV | Status: pending

## TR-CORE-HLTH-001: Health endpoint
/health 200 when process and DB reachable.
Priority: high | Area: CORE | Subarea: HLTH | Status: pending

## TR-CORE-LOG-001: Structured event logging
Logs for post, AI, moderation, bans, background.
Priority: high | Area: CORE | Subarea: LOG | Status: pending

## TR-CORE-DOCKER-001: Multi-stage container
Image on 8080, non-root, healthcheck, /app/data volume docs.
Priority: high | Area: CORE | Subarea: DOCKER | Status: pending

## TR-CORE-PATH-001: PathBase
PATH_BASE=/x support with base href.
Priority: high | Area: CORE | Subarea: PATH | Status: pending

## TR-CORE-API-001: Client data APIs
Feed read, post, vote, share, health for Blazor client; no client AI keys.
Priority: high | Area: CORE | Subarea: API | Status: pending

## TR-CORE-OWN-001: Unique implementation ownership
No sibling src/tests as compile/copy inputs.
Priority: critical | Area: CORE | Subarea: OWN | Status: pending

## TR-CORE-EXPORT-001: Export bundle contract
Versioned export/restore for EternalX entities; reject unsupported versions.
Priority: medium | Area: CORE | Subarea: EXPORT | Status: pending
Notes: From Reddit TR-CORE-EXPORT-001 adapted.

---

## Tests

## TEST-AI-001: Roster and generation unit coverage
Unit tests for figure picks, peer-group fallback, persona threading into prompts, provider/model metadata, and optional scripted gag durability under concurrent writes (mocked providers).
Priority: high | Area: AI | Status: pending
Notes: From Reddit TEST-AI-001 adapted.

## TEST-AI-002: Deep thread and moderation path
Mocked AI: bounded 5-7 replies after post; each passes moderation; injection/NSFW paths verified.
Priority: critical | Area: AI | Status: pending

## TEST-AI-003: AutoReply policy gates
AutoReplyPolicy tests: quiet period, max replies, tick limit, prompt truncation; simulated ticks cannot exceed cap.
Priority: critical | Area: AI | Status: pending

## TEST-AI-004: Server-side AI isolation
AI not callable from client; DEFAULT_AI_PROVIDER selection with mocks.
Priority: critical | Area: AI | Status: pending

## TEST-CORE-001: Data store and auth matrix
LiteDB round-trips, gateway header trust, OIDC config presence, vote/share authz, rate limit, concurrency no lost updates.
Priority: critical | Area: CORE | Status: pending
Notes: From Reddit TEST-CORE-001 adapted (no Reddit admin sub matrix).

## TEST-CORE-002: PathBase and health
PATH_BASE and forwarded headers; /health behavior.
Priority: high | Area: CORE | Status: pending

## TEST-CORE-003: Deploy script and docker contract
octopus-deploy.ps1 parse/CWD resolution; Dockerfile 8080 healthcheck metadata.
Priority: medium | Area: CORE | Status: pending
Notes: From Reddit TEST-CORE-003 adapted to this repo only.

## TEST-CORE-004: Seed and export
Idempotent seed; export/restore version rejection when implemented.
Priority: medium | Area: CORE | Status: pending
Notes: From Reddit TEST-CORE-004 adapted.
