# Testing Requirements (MCP Server)

## TEST-AI

### TEST-AI-001

Unit tests for figure picks, peer-group fallback, persona into prompts, provider/model metadata, optional scripted gag under concurrent writes with mocked providers.


### TEST-AI-002

Mocked AI: bounded 5-7 replies after post; each moderated; injection/NSFW paths verified.


### TEST-AI-003

AutoReplyPolicy: quiet period, max replies, tick limit, prompt truncation; simulated ticks cannot exceed cap.


### TEST-AI-004

AI not callable from client; DEFAULT_AI_PROVIDER with mocks.



## TEST-CORE

### TEST-CORE-001

LiteDB round-trips, gateway header trust (key match and spoof rejection), vote/share authz, rate limit, concurrency no lost updates. Must not register local OIDC schemes.


### TEST-CORE-002

PATH_BASE and forwarded headers; /health behavior.


### TEST-CORE-003

octopus-deploy.ps1 parse/CWD; Dockerfile 8080 healthcheck metadata.


### TEST-CORE-004

Idempotent seed; export/restore version rejection when implemented.



## TEST-UI

### TEST-UI-001

Unit tests for Blazor components must cover feed list ordering, composer visibility when authenticated vs anonymous, and thread nesting presentation for the X-style shell.
