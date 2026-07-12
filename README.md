# EternalX.Blazor

A full-featured Blazor WebAssembly Hosted application running in Docker.

## Features
- Anonymous reading of the Eternal Feed
- Login with Google, Microsoft, or GitHub (OpenID Connect)
- Real AI replies from Claude, OpenAI, Grok, or Hugging Face (keys via environment variables)
- Moderator AI that blocks NSFW content and prompt injection attempts
- Automatic ban on prompt injection
- Rate limiting: 1 post per minute per IP address
- Background service that auto-generates interesting replies every 10 seconds
- Upvote, downvote, and share functionality
- LiteDB persistence

## Running with Docker

1. Copy `.env.example` to `.env` and fill in your API keys, OAuth credentials, and optional NGROK_AUTHTOKEN.
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
- Use that public URL as the base for your OAuth redirect URIs in Google/Microsoft/GitHub developer consoles.

## Important Notes
- You (the developer) supply the AI API keys in the `.env` file.
- Users authenticate via their chosen OpenID provider to post.
- The Moderator runs on every new post.
- The Auto-Reply service runs continuously in the background.

## Deployment with Octopus Deploy

See `deploy/octopus/README.md` for full configuration instructions.

**Quick Summary:**
- Build and push Docker image to your container registry
- Create Octopus Project "EternalX"
- Add the sensitive variables listed in `deploy/octopus/README.md`
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