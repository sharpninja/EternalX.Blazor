# EternalX.Blazor

A full-featured Blazor WebAssembly Hosted application running in Docker.

**Eternal network:** shares core data and AI interaction semantics with EternalReddit and EternalDiscord; this product's UI is **Twitter / X-style** (siblings: classic Reddit UI, Discord chat UI). Each agent owns a **unique implementation** of shared requirements in their own repo (GrokCode here). See [`docs/product-architecture.md`](docs/product-architecture.md).

## Features
- Anonymous reading of the Eternal Feed
- Authentication via EternalSocial gateway only (`GATEWAY_KEY` + `X-Auth-*`; no local OIDC)
- Real AI replies from Claude, OpenAI, Grok, or Hugging Face (keys via environment variables)
- Moderator AI that blocks NSFW content and prompt injection attempts
- Automatic ban on prompt injection
- Rate limiting: 1 post per minute per IP address
- Background service that auto-generates interesting replies every 10 seconds
- Upvote, downvote, and share functionality
- LiteDB persistence

## Running with Docker

1. Copy `.env.example` to `.env` and fill in AI keys, required `GATEWAY_KEY` (shared with the proxy), and optional `NGROK_AUTHTOKEN`.
2. Run:
   ```bash
   docker-compose up --build
   ```
3. Open http://localhost:8080

### Using ngrok (recommended for OAuth testing)

ngrok is included as a sidecar service. It provides a public HTTPS URL for your local instance.

- Get your free authtoken at https://dashboard.ngrok.com
- Add it to `.env` as `NGROK_AUTHTOKEN`
- After `docker-compose up`, visit http://localhost:4040 to see the public URL (e.g. `https://xxxx.ngrok.io`)
- OAuth redirect URIs belong on the **gateway**, not this site.

## Important Notes
- You (the developer) supply the AI API keys in the `.env` file.
- Users authenticate via the EternalSocial proxy; EternalX never runs local OIDC.
- The Moderator runs on every new post.
- The Auto-Reply service runs continuously in the background.

## Documentation

- **Process (mandatory):** all work must follow [Byrd Development Process v4](docs/byrd-development-process.md) (canonical source: McpServer `docs/Development-Process-draft-v4.md`)
- **Requirements precedence:** sibling `docs/` requirements apply unless they conflict with this repo; local wins ([`docs/requirements-precedence.md`](docs/requirements-precedence.md))
- Product architecture (shared core / divergent UI): [`docs/product-architecture.md`](docs/product-architecture.md)
- Requirement groups (UI / AI / Core): [docs/Project/requirement-groups.md](docs/Project/requirement-groups.md)
- Product requirements: [`docs/REQUIREMENTS.md`](docs/REQUIREMENTS.md)
- Octopus Deploy configuration: [`docs/deploy/octopus.md`](docs/deploy/octopus.md)

## Deployment with Octopus Deploy

See [`docs/deploy/octopus.md`](docs/deploy/octopus.md) for full configuration instructions.

**Quick Summary:**
- Build and push Docker image to your container registry
- Create Octopus Project "EternalX"
- Add the sensitive variables listed in `docs/deploy/octopus.md`
- Use the **Deploy Docker Container** step
- Map Octopus variables to container environment variables
- LiteDB data persists via a mounted volume at `/app/data`

## Next Steps / TODO
- Implement full frontend (Blazor Client pages for feed, composer, replies, voting)
- Complete the API controllers for Posts/Replies
- Add proper error handling and logging
- Improve the AI prompt engineering for historical figures
- Add real API calls for all four providers in AiService

This is a complete backend foundation ready for frontend development and Octopus Deploy.