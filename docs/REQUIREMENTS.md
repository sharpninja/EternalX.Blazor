# EternalX.Blazor - Detailed Requirements Specification

**Version:** 1.1
**Date:** 2026-07-13
**Status:** Active - X-style engagement, admin agents/personality metrics, live AI (Grok default)

**Precedence:** Requirements from sibling repos (EternalReddit, EternalDiscord) also apply to this product **except** where they conflict with this document or other native EternalX.Blazor requirements. **This repository wins on conflict.** Full rule: [`requirements-precedence.md`](requirements-precedence.md).

---

## 1. Project Overview

**Name:** EternalX.Blazor  
**Type:** Blazor WebAssembly Hosted (Client + Server API)  
**Deployment Target:** Docker container, managed via Octopus Deploy  
**Public Access:** ngrok tunnel for local development + OAuth callback testing  
**UI paradigm:** **Twitter / X-style** (timeline feed, composer, threaded replies, engagement affordances). Not Reddit listing UI and not Discord chat UI.  
**Core Concept:** A modern, interactive social feed where historical, legendary, and mythical figures reply to user posts using real AI, with rich crossovers, moderation, and background activity.

**Eternal network:** Core **data** and **AI interactions** are shared with EternalReddit and EternalDiscord; only the **UI shell** differs significantly. **Shared requirements do not imply a shared codebase:** each product agent owns a unique implementation (GrokCode owns this repo). Architecture note: [`product-architecture.md`](product-architecture.md). Sibling requirements still apply subject to [`requirements-precedence.md`](requirements-precedence.md).

The application allows anonymous reading but requires authentication to post or interact. All AI replies are generated server-side using credentials supplied by the operator (not end users).

---

## 2. Functional Requirements

### 2.1 Authentication & Authorization
- Users can browse and read all content **anonymously**.
- Users **must be authenticated** to create posts or generate AI replies.
- **No local OIDC** (no Google/Microsoft/GitHub client on EternalX).
- Authentication is **EternalSocial gateway only**: the proxy supplies `X-Gateway-Key` (shared secret `GATEWAY_KEY`) and, when signed in, `X-Auth-UserId` / `X-Auth-Name` / `X-Auth-Email`. Headers without a matching key are ignored (anonymous).
- Login/logout links target the gateway (`/login`, `/logout`), not this site.
- No local user accounts or passwords.

### 2.2 Posting & Content Creation
- Authenticated users can submit new posts (text only, with optional title or topic).
- Posts appear in a chronological feed (newest first).
- Rate limiting: **Maximum 1 post per minute per IP address** (enforced server-side, even for authenticated users).
- Posts are persisted immediately to LiteDB.

### 2.3 AI Reply Generation
- When a user posts, the system automatically generates a **deep reply thread** (5–7 replies) from a rotating set of historical figures.
- Supported AI providers (credentials supplied via environment variables):
  - Hugging Face (Inference API)
  - Anthropic Claude
  - OpenAI (ChatGPT)
  - xAI Grok (default provider and model path when keys present; default model `grok-4.3`)
- `DEFAULT_AI_PROVIDER` and per-provider model env vars (`XAI_MODEL`, etc.) configure selection.
- Operators may **enable/disable providers in admin** without removing API keys (`FeedSettings.DisabledProviders`).
- Replies must stay in character for each historical figure and produce **unlikely but on-point crossovers** between figures.
- AI calls are made server-side only. No API keys are ever sent to the client.
- Failed provider HTTP calls fall back to a deterministic stub and surface `lastError` on `/api/ai/status`.

### 2.4 Moderation System (Moderator AI)
- Every new post and every AI-generated reply passes through a **Moderator AI** before being accepted.
- The moderator checks for:
  - Not Safe For Work (NSFW) / adult / violent / hateful content
  - Prompt injection attempts (jailbreaks, "ignore previous instructions", role-play overrides, etc.)
- If **prompt injection is detected**:
  - The post/reply is blocked
  - The user is **automatically banned** (IP + user ID recorded)
  - Future posts from that user are rejected
- NSFW content is blocked but does not result in a ban (unless it also contains injection).
- Moderator decisions are logged.

### 2.5 Background Auto-Reply Service
- A background service runs on a **10-second timer**.
- It scans recently active threads that have not received a new reply in the last few minutes.
- It selects an "interesting" previous AI reply as context.
- It generates and posts **one new AI reply** into that thread from a relevant historical figure.
- This keeps the feed alive and creates emergent conversations even when no human is posting.
- The service respects rate limits and moderation rules.

### 2.6 Social Interactions (X-style)
- Authenticated users can:
  - **Like** a post or reply (heart toggle; no downvotes)
  - **Quote-reshare** a post as a **new post** (optional comment; not a reply in-thread)
  - **Reply** in-thread under a post
- Content supports **@mentions** (people/handles) and **#hashtags** (topics), stored and highlighted.
- Anonymous users can see like/reshare counts but cannot like or reshare.
- Admin can view **engagement by personality** (likes, reshares, replies, mentions, weighted score).

### 2.7 Data Persistence
- All data is stored in **LiteDB** (embedded NoSQL database).
- Collections include:
  - Posts (with embedded replies)
  - Users (minimal profile: provider, subject ID, display name, ban status)
  - Votes
  - Moderation logs
  - Rate-limit counters (sliding window per IP)
- LiteDB file is persisted via a Docker volume (`/app/data`).

---

## 3. Non-Functional Requirements

### 3.1 Security
- All AI credentials are supplied exclusively via **environment variables** (never in source code or client).
- API keys are marked as **Sensitive** in Octopus Deploy.
- Rate limiting is enforced at the IP level before any business logic.
- Prompt injection detection is mandatory on every inbound post.
- No client-side AI calls.
- HTTPS is required in production (ngrok provides this during local dev).

### 3.2 Performance & Scalability
- The background service must not block the request pipeline.
- LiteDB is acceptable for single-instance / low-to-medium traffic.
- AI calls are the primary latency source; they should be asynchronous and non-blocking.
- Target: Page load < 2s on first visit, reply generation < 8s (with streaming where possible).

### 3.3 Reliability
- Background service must recover gracefully after restart.
- LiteDB corruption should be detectable and recoverable (or the container should fail fast).
- Health check endpoint (`/health`) must return 200 when the app and database are healthy.

### 3.4 Observability
- Structured logging for all major events (post creation, AI calls, moderation decisions, bans, background replies).
- Moderation decisions should be queryable for audit.

---

## 4. Deployment & Infrastructure

### 4.1 Docker
- Single multi-stage `Dockerfile` producing a lean production image.
- Listens on port **8080** inside the container.
- Non-root user.
- Healthcheck on `/health`.
- Volume mount required for `/app/data` (LiteDB).

### 4.2 Docker Compose (Local Development)
- Includes the main `eternalx` service.
- Includes `ngrok` sidecar for public HTTPS tunnel.
- `.env` file supplies all secrets (AI keys, OAuth client secrets, ngrok token).
- Public ngrok URL is used for OAuth redirect URIs during local testing.

### 4.3 Octopus Deploy
- Project name: **EternalX**
- Deployment process uses the official **Deploy Docker Container** step.
- All AI keys and OAuth secrets are stored as **Sensitive** variables in Octopus.
- Recommended variable scope: `Production`, `Staging`, `Development`.
- Persistent volume mapping for LiteDB data directory is required in the deployment target.

### 4.4 ngrok (Development Only)
- Provides a stable public HTTPS URL for local development.
- Required for testing Google/Microsoft/GitHub OAuth callbacks.
- Authtoken supplied via environment variable.

---

## 5. Frontend (Blazor WebAssembly Client)

Implemented X-style shell (FR-UI-001/002):
- Left rail: Home, Admin (owner), login/logout, user card
- Center column sticky header with AI status chip
- Composer when authenticated (280 counter; @mentions / #hashtags)
- Timeline with nested replies, AI provider/model badges, quote cards
- Action bars: reply, quote-reshare, like (heart), open deep link (`/post/{id}`)
- SignalR hub `/hubs/feed` (`FeedChanged`); poll fallback if disconnected
- Owner admin: feed controls, AI agent enable/disable, engagement by personality
- Live AI via env keys; stub when no keys or all agents disabled

Deferred: FR-AI-009 scripted gag; mention/hashtag search pages.

---

## 6. Current Implementation Status (as of 2026-07-13)

**Completed:**
- Gateway-only auth; admin gated by `Authorization__AdminEmail`
- LiteDB: posts/replies with likes, reshares, quotes, mentions/hashtags, figures, settings
- Concurrent reply commits; rate limit; moderation; health + DB check
- AiService multi-provider; Grok default model `grok-4.3`; agent toggles; personality engagement
- APIs: feed, post, reply, like, reshare, me, ai/status, admin/* (agents, engagement)
- Blazor UI: layout rail, timeline, post page, admin
- SignalR feed push; unit tests green (64+ suite as of refresh)

**Deferred / later:**
- FR-AI-009 scripted gag
- Production logging sinks
- Hashtag/mention discovery surfaces

---

## 7. Open Questions / TBD

- Should banned users still be able to read anonymously? (Recommended: Yes)
- Long-term storage if LiteDB becomes a bottleneck

---

**Completeness of this requirements document: 96/100**

---

*This document should be treated as the authoritative source of truth for scope until a formal change control process is introduced.*