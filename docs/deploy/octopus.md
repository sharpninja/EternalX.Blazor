# Octopus Deploy Configuration

## Variables (mark Sensitive where noted)

- `CLAUDE_API_KEY` (Sensitive)
- `OPENAI_API_KEY` (Sensitive)
- `GROK_API_KEY` (Sensitive)
- `HUGGINGFACE_API_KEY` (Sensitive)
- `DEFAULT_AI_PROVIDER`
- `GATEWAY_KEY` (Sensitive) - shared with EternalSocial proxy; required for auth
- `PATH_BASE` = `/x` (when behind the gateway)
- `LITEDB_PATH` = `/app/data/eternalx.db`

Do **not** configure site-local Google/Microsoft/GitHub OIDC client secrets. Sign-in is owned by the gateway.

## Deployment Process

1. Build Docker image and push to your container registry
2. Create Octopus Project named "EternalX"
3. Add the variables above (include `GATEWAY_KEY` from the EternalSocial library set)
4. Use **Deploy Docker Container** step type
5. Map Octopus variables to container environment variables
6. Mount a persistent volume for `/app/data` (LiteDB)
7. Join the shared `eternal` docker network; no public host ports in estate mode

## Recommended
- Use a Docker Registry feed in Octopus
- Add health check step after deployment
- Set up variable sets for different environments (dev/staging/prod)
