# Functional Requirements (MCP Server)

## FR-AI-001 Deep reply thread on user post

When a user posts, the system must automatically generate a deep reply thread of 5 to 7 replies from a rotating set of historical, legendary, or mythical figures.
Scope: layer-1+

## FR-AI-002 Multi-provider server-side AI

The system must support AI providers Hugging Face, Anthropic Claude (default), OpenAI, and xAI Grok using operator-supplied credentials via environment variables. All AI calls must be server-side only; API keys must never be sent to the client.
Scope: layer-1+

## FR-AI-003 In-character crossovers

AI replies must stay in character for the assigned figure and produce unlikely but on-point crossovers between figures.
Scope: layer-1+

## FR-ARCH-001 Shared core semantics unique implementation

The EternalX product must implement shared Eternal network core data and AI interaction semantics while remaining a unique implementation owned by GrokCode in this repository. Sibling requirements inform behavior; sibling src and tests must not be used as implementation sources.
Scope: layer-1+

## FR-AUTH-001 Anonymous browse

Users must be able to browse and read all public feed content anonymously without signing in.
Scope: layer-1+

## FR-AUTH-002 Authenticated write and engage

Users must be authenticated to create posts, vote, share, or trigger AI reply generation.
Scope: layer-1+

## FR-AUTH-003 Standalone multi-provider OIDC

When not operating exclusively behind gateway-enforced SSO, EternalX must support server-side OpenID Connect login with Google, Microsoft (Entra ID), and GitHub. No local username/password accounts. Session must support posting, voting, and sharing. Local REQUIREMENTS override sibling gateway-only OAuth prohibition for standalone mode.
Scope: layer-1+

## FR-AUTH-004 Optional EternalSocial gateway identity

When GATEWAY_KEY is configured, EternalX must accept identity only when request X-Gateway-Key matches, building principal from X-Auth-UserId, X-Auth-Name, and X-Auth-Email. Mismatched or missing key yields anonymous. Login/logout in gateway deployments target gateway /login and /logout. Path base and forwarded headers must work behind the gateway.
Scope: layer-1+

## FR-BG-001 Background auto-reply service

A background service on a 10-second timer must scan recently active threads quiet for a few minutes, select interesting AI context, and post one new in-character AI reply, respecting rate limits and moderation including moderating background replies.
Scope: layer-1+

## FR-DATA-001 LiteDB persistence

All application data must persist in LiteDB with collections for posts with embedded replies, users, votes, moderation logs, and rate-limit counters. The database file must survive container restarts via volume mount at /app/data.
Scope: layer-1+

## FR-DEPLOY-001 Docker runtime

EternalX must ship as a multi-stage Docker image listening on 8080, non-root, healthcheck on /health, with volume for /app/data. Local docker-compose must include the app and optional ngrok sidecar with .env secrets.
Scope: layer-1+

## FR-DEPLOY-002 Octopus project EternalX

Deployment must support Octopus project EternalX with Deploy Docker Container step, Sensitive variables for AI and OAuth secrets, DEFAULT_AI_PROVIDER, LITEDB_PATH, environment scopes Development/Staging/Production, and persistent volume for LiteDB.
Scope: layer-1+

## FR-DEPLOY-003 Independent per-repo deploy trigger

EternalX must deploy independently via Octopus Git trigger on this repo push to main without requiring a shared multi-repo pipeline.
Scope: layer-1+

## FR-MOD-001 Moderate all content

Every new user post and every AI-generated reply must pass through Moderator AI before acceptance.
Scope: layer-1+

## FR-MOD-002 Prompt injection ban

If prompt injection is detected, the content must be blocked, the user must be auto-banned (IP and user ID recorded), and future posts from that user must be rejected.
Scope: layer-1+

## FR-MOD-003 NSFW block without ban

NSFW, adult, violent, or hateful content must be blocked without banning the user unless prompt injection is also present. Moderator decisions must be logged.
Scope: layer-1+

## FR-NET-001 Path base behind gateway

When deployed under the EternalSocial gateway path prefix /x, EternalX must absorb PATH_BASE / Proxy:PathBase with matching document base href and trust X-Forwarded-Proto/Host from the gateway.
Scope: layer-1+

## FR-OPS-001 Health and observability

The app must expose /health returning 200 when app and database are healthy. Structured logging must cover post creation, AI calls, moderation decisions, bans, and background replies. Moderation decisions must be queryable for audit.
Scope: layer-1+

## FR-POST-001 Create text posts

Authenticated users must be able to submit new text posts with optional title or topic; posts persist immediately.
Scope: layer-1+

## FR-POST-002 Chronological feed

Posts must appear in a chronological feed newest first.
Scope: layer-1+

## FR-POST-003 IP post rate limit

The server must enforce a maximum of one new post per minute per IP address for all users including authenticated users.
Scope: layer-1+

## FR-SEC-001 Secrets and HTTPS

AI credentials and OAuth secrets must come only from environment variables (Sensitive in Octopus). No client-side AI calls. Production must use HTTPS; local dev may use ngrok for HTTPS and OAuth callbacks.
Scope: layer-1+

## FR-SOC-001 Upvote and downvote

Authenticated users must be able to upvote (+1) and downvote (-1) posts and replies; counts must be displayed and persisted. Anonymous users may view counts but not vote.
Scope: layer-1+

## FR-SOC-002 Share deep links

Authenticated users must be able to share a post or specific reply via a clean shareable link that opens directly to that item; share counts must be displayed and persisted.
Scope: layer-1+

## FR-UI-001 Twitter X style user interface

EternalX must present a Twitter/X-style UI (timeline feed, composer, threaded replies, engagement affordances). It must not adopt classic Reddit listing/sub chrome or Discord chat chrome.
Scope: layer-1+

## FR-UI-002 Blazor client shell

The Blazor WebAssembly client must provide top nav with logo and login/logout, composer when authenticated, main feed with nested reply threads, action bars, read-only AI provider status, minimal profile/ban display, share UX, responsive layout, and near-real-time updates without full page refresh.
Scope: layer-1+

