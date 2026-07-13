# Testing Requirements (MCP Server)

## TEST-AI

### TEST-AI-001

Tests must verify AI service is not registered for client-side invocation and that provider selection respects DEFAULT_AI_PROVIDER with mocked providers.


### TEST-AI-002

Tests with mocked AI must verify 5 to 7 reply generation path is invoked after accepted user post and each reply is submitted through moderation.



## TEST-API

### TEST-API-001

Integration tests must verify anonymous feed read, authenticated post, and rejection of client attempts to pass AI API keys.



## TEST-ARCH

### TEST-ARCH-001

Automated checks or review gate must fail if build scripts or code references sibling repo src/tests paths as compile or copy inputs for EternalX.



## TEST-AUTH

### TEST-AUTH-001

Tests must verify anonymous read is allowed, write endpoints return 401 when unauthenticated, and OIDC configuration registers Google, Microsoft, and GitHub schemes.


### TEST-AUTH-002

When GATEWAY_KEY is configured, tests must verify principal is established only with matching X-Gateway-Key plus X-Auth headers, and mismatched key yields anonymous.



## TEST-BG

### TEST-BG-001

Tests must verify AutoReplyBackgroundService selects a candidate thread and posts one moderated reply using mocks without blocking a sample HTTP request.



## TEST-DATA

### TEST-DATA-001

Tests must verify posts, replies, users, votes, and moderation logs round-trip through LiteDbService and survive reopen of the database file.


### TEST-DATA-002

Concurrency tests must verify interleaved user and background replies do not drop posts or lose reply threads.



## TEST-DEPLOY

### TEST-DEPLOY-001

Tests or CI checks must validate octopus-deploy.ps1 parses under PowerShell, resolves repo root from script-folder CWD, and Dockerfile exposes 8080 with healthcheck metadata.



## TEST-MOD

### TEST-MOD-001

Tests must verify prompt injection content is rejected and creates ban state, NSFW-only content is rejected without ban, and clean content is accepted.



## TEST-NET

### TEST-NET-001

Tests must verify path base absorption and that forwarded proto/host influence generated public URLs when configured.



## TEST-OPS

### TEST-OPS-001

Tests must verify /health returns 200 when dependencies are healthy and fails when database is unavailable if that failure mode is detectable.



## TEST-POST

### TEST-POST-001

Tests must verify authenticated post create persists content and that a second post from the same IP within one minute is rejected.



## TEST-SOC

### TEST-SOC-001

Tests must verify anonymous vote/share rejected, authenticated upvote/downvote updates scores, and share link generation returns deep link targets.



## TEST-UI

### TEST-UI-001

Unit tests for Blazor components must cover feed list ordering, composer visibility when authenticated vs anonymous, and thread nesting presentation for the X-style shell.
