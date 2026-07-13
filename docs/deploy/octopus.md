# Octopus Deploy Configuration

## Shared secrets with EternalReddit / EternalReadit

EternalX does **not** need a second set of Claude/Grok secrets. Reuse the same
Octopus Sensitive variables already used by EternalReadit (project variables or a
shared library variable set included on the EternalX project).

Canonical names (match EternalReadit `deploy/octopus-deploy.ps1`):

| Octopus variable | Purpose | Required |
|------------------|---------|----------|
| `GATEWAY_KEY` | EternalSocial library set; SSO trust | **Yes** |
| `ANTHROPIC_API_KEY` | Claude (live AI) | For live Claude |
| `XAI_API_KEY` | Grok / xAI (live AI) | For live Grok |
| `OPENAI_API_KEY` | OpenAI (optional) | No |
| `HF_API_KEY` | Hugging Face (optional) | No |

App also accepts aliases if you already named keys differently in Octopus:

- `CLAUDE_API_KEY` (same as Anthropic)
- `GROK_API_KEY` (same as xAI)
- `HUGGINGFACE_API_KEY` (same as HF)

`deploy/octopus-deploy.ps1` writes whatever is present into a temporary env file
and passes `--env-file` into the container (same pattern as EternalReadit).

## Optional settings

- `DEFAULT_AI_PROVIDER` = `claude` | `openai` | `grok` | `huggingface`
- `ANTHROPIC_MODEL` / `CLAUDE_MODEL`, `XAI_MODEL` / `GROK_MODEL`, `OPENAI_MODEL`
- `Authorization__AdminEmail` - **required for Admin nav/API**. Must match the gateway `X-Auth-Email` for the owner (sibling default: `plbyrd@gmail.com`). Without this, the left-rail Admin link never appears and `/api/admin/*` returns 403.
- `PATH_BASE` = `/x` (hard-coded in the deploy script for estate mode)
- `LITEDB_PATH` = `/app/data/eternalx.db` (default inside image)

Do **not** configure site-local Google/Microsoft/GitHub OIDC client secrets. Sign-in is owned by the gateway.

## Project wiring checklist

1. Octopus project **EternalX**
2. Include the same library variable set(s) as EternalReadit for `GATEWAY_KEY` and AI keys (or copy Sensitive values once into EternalX project scope)
3. Git trigger on this repo `main` → Development auto-deploy
4. Run step: `deploy/octopus-deploy.ps1` (CWD = `deploy/` under git-sourced package)
5. Named volume `eternalx-data` → `/app/data`
6. Network `eternal`; **no public host ports** (gateway only)

After deploy, `/api/ai/status` should show `"live": true` and `liveProviders` including `claude` and/or `grok` when those keys were injected. `"usingStub": true` means the container did not receive keys (library set not linked, or empty values).

## Deployment Process

1. Build is performed by `octopus-deploy.ps1` (`docker build` on the target)
2. Container `eternalx` on network `eternal`, volume `eternalx-data`
3. Health: `GET /health` through the gateway path `/x/health` (or container-local)

## Recommended

- Prefer one shared **library** variable set for AI keys + `GATEWAY_KEY` on all Eternal* projects so keys stay single-sourced
- Mark all API keys and `GATEWAY_KEY` as **Sensitive**
- Smoke after deploy: gateway `/x`, health 200, AI status `live: true`
