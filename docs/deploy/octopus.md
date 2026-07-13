# Octopus Deploy Configuration

## Variables (mark Sensitive where noted)

- `CLAUDE_API_KEY` (Sensitive)
- `OPENAI_API_KEY` (Sensitive)
- `GROK_API_KEY` (Sensitive)
- `HUGGINGFACE_API_KEY` (Sensitive)
- `DEFAULT_AI_PROVIDER`
- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET` (Sensitive)
- `MICROSOFT_CLIENT_ID`
- `MICROSOFT_CLIENT_SECRET` (Sensitive)
- `GITHUB_CLIENT_ID`
- `GITHUB_CLIENT_SECRET` (Sensitive)
- `LITEDB_PATH` = `/app/data/eternalx.db`

## Deployment Process

1. Build Docker image and push to your container registry
2. Create Octopus Project named "EternalX"
3. Add the variables above
4. Use **Deploy Docker Container** step type
5. Map Octopus variables to container environment variables
6. Mount a persistent volume for `/app/data` (LiteDB)

## Recommended
- Use a Docker Registry feed in Octopus
- Add health check step after deployment
- Set up variable sets for different environments (dev/staging/prod)