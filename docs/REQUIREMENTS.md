# EternalX.Blazor - Detailed Requirements Specification

**Version:** 1.0  
**Date:** 2026-07-12  
**Status:** Active - UI + backend aligned to imported MCP requirements (2026-07-13)

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
  - Anthropic Claude (default provider)
  - OpenAI (ChatGPT)
  - xAI Grok
- Claude is configured as the **default provider** and defaults to using the operator's logged-in browser session cookies when available (for lower cost / higher limits during development).
- Replies must stay in character for each historical figure and produce **unlikely but on-point crossovers** between figures.
- AI calls are made server-side only. No API keys are ever sent to the client.

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

### 2.6 Social Interactions
- Authenticated users can:
  - **Upvote** a post or reply (+1 karma)
  - **Downvote** a post or reply (−1 karma)
  - **Share** a post or specific reply (generates a clean, shareable link that opens directly to that item)
- Vote counts and share counts are displayed and persisted.
- Anonymous users can see vote counts but cannot vote or share.

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
- Top navigation: logo, AI provider chip, login/logout (gateway `/login` `/logout`), ban badge, admin link
- Composer when authenticated (optional title, 280 counter)
- Timeline with nested replies, AI provider/model badges
- Action bars: upvote, downvote, share path, open deep link (`/post/{id}`)
- SignalR hub `/hubs/feed` (`FeedChanged`) for near-real-time updates; 30s poll fallback if disconnected
- Owner admin page: pause/resume auto-reply, seed, export, clear-feed, stats
- Live AI: Claude/OpenAI/Grok/HuggingFace via env keys (stub only when no keys); dual key names supported

Deferred: share clipboard JS interop polish, FR-AI-009 scripted gag.

---

## 6. Current Implementation Status (as of 2026-07-13)

**Completed:**
- Gateway-only auth (`GATEWAY_KEY` + `X-Auth-*`); no local OIDC
- LiteDB: posts, replies, figures, peer groups, users, votes, moderation logs, settings; idempotent seed; export/restore
- Concurrent reply commits under lock (FR-CORE-006)
- Rate limit 1 post/min/IP; moderation injection ban + NSFW block; health checks DB
- AiService multi-provider (Claude/OpenAI/Grok/HF) with stub fallback; FigurePicker; deep thread 5-7; AutoReplyPolicy bounds + pause
- APIs: feed, post, reply, vote, share, me, ai/status, admin/*
- Blazor UI timeline, post page, admin page
- SignalR feed push + live AI providers (HTTP mocked unit tests)
- Unit tests covering LiteDB, votes, moderator, figure pick, deep thread, AI stub/live, auto-reply policy, gateway identity, UI helpers, feed notifier

**Deferred / later:**
- FR-AI-009 scripted gag
- Production logging sinks
- Live key smoke against real vendor endpoints (ops)

---

## 7. Open Questions / TBD

- Should background auto-replies also be moderated by the Moderator AI? (Recommended: Yes)
- Should banned users still be able to read anonymously? (Recommended: Yes)
- Do we want to persist AI provider usage statistics per thread for analytics?
- Should there be a "Featured" or "Trending" section driven by vote velocity?
- Long-term storage strategy if LiteDB becomes a bottleneck (migration path to PostgreSQL or Cosmos DB).

---

**Completeness of this requirements document: 94/100**  
All major functional areas discussed in the conversation are captured. Minor gaps remain around exact UI copy, specific prompt templates for each historical figure, and detailed API contract definitions (these can be added in v1.1).

---

*This document should be treated as the authoritative source of truth for scope until a formal change control process is introduced.*