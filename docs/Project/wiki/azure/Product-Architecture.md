# Eternal network - shared core, divergent UI

**Status:** Standing product architecture for EternalX.Blazor and its siblings  
**Effective:** 2026-07-13  

## Summary

The three Eternal products share **core data** and **AI interaction** concepts. Their **user interfaces differ significantly** by product, each reproducing a well-known social surface:

- **EternalX** (`F:\GitHub\EternalX.Blazor`, this repo, GrokCode): **Twitter / X-style** feed UI
- **EternalReddit** (`F:\GitHub\EternalReddit`, Claude): **classic Reddit-style** UI
- **EternalDiscord** (`F:\GitHub\EternalDiscord`, Codex): **Discord-style** chat UI

## Shared core (cross-product)

Concepts that are shared across the three repos (requirements and behavior intent; not a license to copy sibling `src/` or `tests/`):

- **Domain data:** users/identity, posts or messages, replies/threads, votes or reactions as each surface maps them, figure/persona roster, moderation outcomes, bans, share targets, and related persistence concerns.
- **AI interactions:** server-side generation with operator-supplied credentials; in-character historical / legendary / mythical figures; crossovers; moderator AI (NSFW / injection); background activity that keeps conversations alive; provider configuration (e.g. Claude, OpenAI, Grok, Hugging Face) as required by each product's docs.
- **Network-level contracts** documented in sibling `docs/` when applicable (e.g. EternalSocial gateway SSO), subject to [`requirements-precedence.md`](requirements-precedence.md).

Shared core means **aligned requirements and semantics**, not identical UI chrome or a forced single codebase. Implementation of shared behavior in this repo must still follow Byrd Dev Process v4 (requirements, tests first, green gates, receipts).

## Implementation ownership (mandatory)

Even though **requirements** are shared (subject to local precedence), **each agent is fully responsible for their own unique implementation** in their assigned repository.

- **GrokCode** owns all design, code, tests, and validation in **EternalX.Blazor**.
- **Claude** owns all design, code, tests, and validation in **EternalReddit**.
- **Codex** owns all design, code, tests, and validation in **EternalDiscord**.

Implications:

1. **No implementation borrowing.** Do not copy, port, or "sync" production code or tests from a sibling `src/` or `tests/` tree. Sibling **docs** may inform *what* must be true; *how* it is built is original work in this repo.
2. **No shared ownership of defects.** A bug or missing feature in this product is GrokCode's to fix here under BDP v4, even if siblings already solved a similar problem.
3. **Unique stack and design are expected.** Different UIs (and potentially different internal structure) are correct. Convergence of external behavior with sibling requirements is success; identical internal structure is not a goal.
4. **Cross-agent coordination** is via operator direction, shared docs, and requirement IDs - not by editing another agent's repo or treating their code as a library unless the operator explicitly adds a shared package or API contract later.

## Divergent UI (product-specific)

### EternalX (this repository)

- Reproduces a **Twitter-based (X-style)** UI: left rail, timeline/feed, composer, threaded replies, hearts (likes), quote-reshares as new posts, @mentions and #hashtags, owner admin (agents + personality engagement).
- Native product requirements: [`REQUIREMENTS.md`](REQUIREMENTS.md).
- Frontend work in this repo optimizes for the X/Twitter interaction model, not Reddit listing/sub community chrome or Discord channel/server chrome.

### EternalReddit

- Reproduces a **classic Reddit** UI: community/sub structure, post listings, comment trees, karma-oriented presentation, and related classic Reddit patterns.
- Requirements and contracts: sibling `docs/` only from this workspace (including MCP wiki exports under `docs/Project/wiki/` when present).

### EternalDiscord

- Reproduces a **Discord chat** UI: server/channel-style navigation, chat transcript presentation, and real-time chat interaction patterns.
- Requirements: sibling `docs/` only from this workspace.

## Implications for work in EternalX.Blazor

1. **Prefer shared semantics** for data and AI behavior when sibling docs define them, unless local requirements conflict (local wins).
2. **Own the full EternalX implementation** (GrokCode): design, code, tests, green gates, receipts. Shared requirements do not reduce that responsibility.
3. **Do not** import Reddit or Discord UI structure into EternalX. Keep the Twitter/X paradigm for client pages, layout, and interaction design.
4. **Do not** use sibling `src/` or `tests/` as copy-paste sources. Read sibling **docs** for shared contracts; implement and test **here**, uniquely.
5. When a feature is "shared core," write FR/TR/TEST that describe the shared behavior, then UI-specific acceptance criteria for the X-style surface.
6. Log cross-product design decisions (what is shared vs UI-local vs implementation-local) in the session log as design decisions, not only as actions.

## Related

- Requirements precedence: [`requirements-precedence.md`](requirements-precedence.md)
- Process: [`byrd-development-process.md`](byrd-development-process.md)
- Local product requirements: [`REQUIREMENTS.md`](REQUIREMENTS.md)
- Agent conduct: [`../AGENTS.md`](../AGENTS.md)
