#Requires -Version 7.0
# Import EternalReddit wiki AI + CORE requirements adapted for EternalX into MCP.
$ErrorActionPreference = 'Stop'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_ROOT = $env:GROK_PLUGIN_ROOT
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_ROOT = $env:GROK_PLUGIN_ROOT
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\EternalX.Blazor'
$env:MCP_AGENT_NAME = 'GrokCode'
Set-Location 'F:\GitHub\EternalX.Blazor'
$plugin = Join-Path $env:GROK_PLUGIN_ROOT 'lib\Invoke-McpPlugin.ps1'
$ws = 'F:\GitHub\EternalX.Blazor'

function Invoke-Mcp {
    param([string]$Method, [object]$ParamsObject)
    & $plugin -Command Invoke -Method $Method -ParamsObject $ParamsObject -WorkspacePath $ws -TimeoutSeconds 120
}

function Get-IdsFromList {
    param([string]$Method)
    $out = & $plugin -Command Invoke -Method $Method -Params '' -WorkspacePath $ws -TimeoutSeconds 90 | Out-String
    $ids = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($out -split "`n")) {
        if ($line -match '^\s+- id:\s*(\S+)') { [void]$ids.Add($Matches[1]) }
    }
    return , $ids.ToArray()
}

function Get-AreaFromId {
    param([string]$Id, [string]$Prefix)
    if ($Id -match "^$Prefix-([A-Z]+)-") { return $Matches[1] }
    return 'CORE'
}

function New-Fr {
    param($Id, $Title, $Desc, $Priority, $Notes)
    return @{
        id          = $Id
        title       = $Title
        description = $Desc
        priority    = $Priority
        area        = (Get-AreaFromId -Id $Id -Prefix 'FR')
        notes       = $Notes
    }
}

function New-Tr {
    param($Id, $Title, $Desc, $Priority, $Subarea, $Notes)
    return @{
        id          = $Id
        title       = $Title
        description = $Desc
        priority    = $Priority
        area        = (Get-AreaFromId -Id $Id -Prefix 'TR')
        subarea     = $Subarea
        notes       = $Notes
    }
}

function New-Test {
    param($Id, $Title, $Desc, $Priority, $Notes)
    return @{
        id          = $Id
        title       = $Title
        description = $Desc
        priority    = $Priority
        area        = (Get-AreaFromId -Id $Id -Prefix 'TEST')
        notes       = $Notes
    }
}

function Split-Upsert {
    param($Records, $Existing, $CreateMethod, $UpdateMethod)
    $toCreate = [System.Collections.Generic.List[object]]::new()
    $toUpdate = [System.Collections.Generic.List[object]]::new()
    foreach ($r in $Records) {
        if ($Existing -contains $r.id) { [void]$toUpdate.Add($r) } else { [void]$toCreate.Add($r) }
    }
    Write-Host "$CreateMethod create=$($toCreate.Count) update=$($toUpdate.Count)"
    if ($toCreate.Count -gt 0) { Invoke-Mcp $CreateMethod @{ records = @($toCreate.ToArray()) } | Out-Null }
    if ($toUpdate.Count -gt 0) { Invoke-Mcp $UpdateMethod @{ records = @($toUpdate.ToArray()) } | Out-Null }
}

$src = 'EternalReddit wiki github + EternalX adapt'
$existingFr = Get-IdsFromList 'workflow.requirements.listFr'
$existingTr = Get-IdsFromList 'workflow.requirements.listTr'
$existingTest = Get-IdsFromList 'workflow.requirements.listTest'
Write-Host "existing FR=$($existingFr.Count) TR=$($existingTr.Count) TEST=$($existingTest.Count)"

$frs = @(
    (New-Fr 'FR-AI-001' 'Per-provider model and effort selection' 'Admins or operator config set AI model and reasoning effort per provider for the EternalX feed via configuration or admin surface populated from live provider catalogs, with fallback to provider env defaults. Reply metadata displays provider and model.' 'high' "$src; Reddit FR-AI-001 adapted feed-level not per-sub"),
    (New-Fr 'FR-AI-002' 'AI feed control' 'Operators can pause and resume auto-reply and auto-post background services, seed content on demand, and view stats. Auto-replies respect quiet gap and must not unbounded-loop. If feed idle about 1 hour, a figure may create an original post.' 'high' "$src; Reddit FR-AI-002 + AutoReplyPolicy"),
    (New-Fr 'FR-AI-003' 'User post submission with AI replies' 'Authenticated users submit posts (text required; title optional for X). AI figures reply in-character on a realistic cadence. Providers may round-robin (Claude, Grok, OpenAI, HuggingFace). Deep initial threads bounded (target 5-7).' 'critical' "$src; Reddit FR-AI-003; title optional local override"),
    (New-Fr 'FR-AI-004' 'Content boundaries and roster curation' 'Figures whose primary legacy is inseparable from atrocity or oppression are excluded. Satire affectionate and hypothetical. Replies only from approved roster. Scripted gags if any are documented exceptions.' 'high' "$src; Reddit FR-AI-004"),
    (New-Fr 'FR-AI-005' 'Curated historical cast' 'Curated roster of historical, legendary, and mythical figures with name and rich persona. Membership data-driven.' 'high' "$src; Reddit FR-AI-005"),
    (New-Fr 'FR-AI-006' 'In-character generation' 'Server assigns figure and persona; models never choose identity. In-character replies, biographical humor, playful tone, peer crossovers.' 'critical' "$src; Reddit FR-AI-006"),
    (New-Fr 'FR-AI-007' 'Peer-group membership for figure picks' 'Peer groups organize the cast; multi-membership allowed. AI picks from allowed groups with fallback so feed never dead-ends.' 'medium' "$src; Reddit FR-AI-007 without Reddit sub modes"),
    (New-Fr 'FR-AI-008' 'Figure lifecycle' 'Roster presence means approved; unapproved names purged. Enabled flag benches picks without deleting history. Operators manage figures and groups.' 'medium' "$src; Reddit FR-AI-008"),
    (New-Fr 'FR-AI-009' 'Scripted gag exception optional' 'Optional scripted non-model gags if enabled must be documented exceptions exempt from model generation and unapproved purge.' 'low' "$src; Reddit FR-AI-009 optional for X"),
    (New-Fr 'FR-AI-010' 'Moderate all content' 'Every user post and AI reply passes moderation before acceptance (NSFW/hate and prompt-injection).' 'critical' 'Local EternalX'),
    (New-Fr 'FR-AI-011' 'Prompt injection ban' 'Injection blocks content, auto-bans user (IP and id), rejects future posts.' 'critical' 'Local EternalX'),
    (New-Fr 'FR-AI-012' 'NSFW block without ban' 'NSFW/hate blocked without ban unless injection also present; decisions logged.' 'high' 'Local EternalX'),
    (New-Fr 'FR-AI-013' 'Background auto-reply bounds' 'Background auto-reply enforces max replies per post, min quiet period, max replies per tick, and max context chars to prevent unbounded growth.' 'critical' 'EternalX production incident AutoReplyPolicy'),
    (New-Fr 'FR-CORE-001' 'Timeline feed' 'EternalX presents a chronological or ranking-capable timeline of posts and threaded replies (X-style). Reddit multi-sub communities are out of scope for this product UI.' 'critical' "$src; Reddit FR-CORE-001 adapted to X timeline"),
    (New-Fr 'FR-CORE-002' 'Peer groups for figures' 'Figures belong to zero or more peer groups for AI casting. Empty allowlist means open to all approved enabled figures with fallback.' 'high' "$src; Reddit FR-CORE-002"),
    (New-Fr 'FR-CORE-003' 'Authenticated human posts and replies' 'Authenticated users post and reply. Humans have identity, no AI provider badge, survive restarts (purge-exempt).' 'critical' "$src; Reddit FR-CORE-003"),
    (New-Fr 'FR-CORE-004' 'Owner-gated admin surface' 'Admin UI and APIs restricted to owner account (Authorization__AdminEmail). Server-side enforcement.' 'high' "$src; Reddit FR-CORE-004"),
    (New-Fr 'FR-CORE-005' 'Admin data tools' 'Admin provides export, restore, and clear-feed (or equivalent) for EternalX data.' 'medium' "$src; Reddit FR-CORE-005"),
    (New-Fr 'FR-CORE-006' 'No lost updates under concurrency' 'Concurrent user and background writes must not drop posts or reply threads.' 'critical' "$src; Reddit FR-CORE-007"),
    (New-Fr 'FR-CORE-007' 'Voting' 'Authenticated users at most one vote per post/reply. Scores displayed and persisted. Anonymous read-only scores.' 'high' "$src; Reddit FR-CORE-008"),
    (New-Fr 'FR-CORE-008' 'Share deep links' 'Authenticated users share post/reply via clean deep link; share counts optional.' 'medium' 'Local EternalX'),
    (New-Fr 'FR-CORE-009' 'Gateway estate participation' 'EternalX participates at /x on eternal docker network. Operates correctly as proxied site; gateway admin owned by gateway product.' 'high' "$src; Reddit FR-CORE-009 site role"),
    (New-Fr 'FR-CORE-010' 'Dual-mode authentication' 'GATEWAY_KEY mode: principal only when X-Gateway-Key matches with X-Auth-*. Standalone: multi-provider OIDC (Google/Microsoft/GitHub). Local dual-mode overrides pure gateway-only sibling wording.' 'critical' "$src; Reddit FR-CORE-010 + local override"),
    (New-Fr 'FR-CORE-011' 'Persistent sign-in' 'Sessions survive visits within cookie lifetime (gateway long-lived cookie or standalone cookie auth).' 'high' "$src; Reddit FR-CORE-012"),
    (New-Fr 'FR-CORE-012' 'Per-repo deploy triggers' 'EternalX deploys independently via Octopus Git trigger on this repo main; no shared multi-repo pipeline required.' 'high' "$src; Reddit FR-CORE-013"),
    (New-Fr 'FR-CORE-013' 'Stable shared GATEWAY_KEY' 'GATEWAY_KEY stable sensitive library variable so EternalX can restart independently without breaking SSO.' 'high' "$src; Reddit FR-CORE-014"),
    (New-Fr 'FR-CORE-014' 'Data-driven content and config' 'Posts/replies, figures, peer groups, users, votes, moderation logs, settings in LiteDB. Prefer data-driven roster/settings.' 'critical' "$src; Reddit FR-CORE-015 X entities"),
    (New-Fr 'FR-CORE-015' 'Durable volume persistence' 'Durable content under /app/data on named volume eternalx-data via LITEDB_PATH.' 'critical' "$src; Reddit FR-CORE-016"),
    (New-Fr 'FR-CORE-016' 'Idempotent seeding' 'First run seeds default figures and peer groups. Re-seed insert-if-absent; no clobber of operator edits. No Reddit 12-community seed required.' 'high' "$src; Reddit FR-CORE-017 adapted"),
    (New-Fr 'FR-CORE-017' 'Versioned export and restore' 'Admin export versioned JSON snapshot; restore rejects unsupported versions; clear-feed preserves roster/config when present.' 'medium' "$src; Reddit FR-CORE-018"),
    (New-Fr 'FR-CORE-018' 'Unique implementation ownership' 'GrokCode owns EternalX design/code/tests/validation. Shared requirements do not authorize sibling src/tests reuse.' 'critical' 'Local architecture'),
    (New-Fr 'FR-CORE-019' 'IP post rate limit' 'Maximum one new post per minute per IP, server-side before side effects.' 'critical' 'Local REQUIREMENTS.md'),
    (New-Fr 'FR-CORE-020' 'Health and observability' '/health 200 when app and DB healthy. Structured logs for posts, AI, moderation, bans, background replies.' 'high' 'Local'),
    (New-Fr 'FR-CORE-021' 'Docker runtime' 'Multi-stage image port 8080, non-root, healthcheck, /app/data volume; compose may include ngrok for standalone OAuth testing.' 'high' 'Local'),
    (New-Fr 'FR-CORE-022' 'Path base and forwarded headers' 'PATH_BASE=/x with UsePathBase and base href; trust gateway X-Forwarded-Proto/Host.' 'high' 'gateway-sso + Reddit TR-CORE-SEC-002'),
    (New-Fr 'FR-CORE-023' 'Secrets from environment only' 'AI and OAuth secrets only via environment or Octopus Sensitive vars; never client AI keys.' 'critical' 'Local')
)

$trs = @(
    (New-Tr 'TR-AI-GEN-001' 'AI selection and generation context' 'Server assigns speaker/persona; models never pick figure. Reply context is post plus ancestor chain truncated as needed. AI comments record provider and model. Background selection avoids unbounded continue-from-last-blob growth.' 'critical' 'GEN' "$src; Reddit TR-AI-GEN-001"),
    (New-Tr 'TR-AI-SEED-001' 'Roster seed mechanics' 'Default roster immutable to callers; seed insert-if-absent before unapproved purge. No Reddit community seed required for EternalX.' 'high' 'SEED' "$src; Reddit TR-AI-SEED-001"),
    (New-Tr 'TR-AI-PROV-001' 'Multi-provider AiService' 'Select provider from DEFAULT_AI_PROVIDER and env keys; server-side only; never expose keys to WASM.' 'critical' 'PROV' 'Local/imported'),
    (New-Tr 'TR-AI-THREAD-001' 'Bounded deep thread generation' 'Post-accept generates bounded deep reply thread (target 5-7) through moderation.' 'critical' 'THREAD' 'Local'),
    (New-Tr 'TR-AI-SCAN-001' 'Moderator pipeline' 'Moderator evaluates NSFW/hate and injection on every user post and AI reply; injection creates ban records.' 'critical' 'SCAN' 'Local'),
    (New-Tr 'TR-AI-TMR-001' 'AutoReplyBackgroundService policy' 'Uses AutoReplyPolicy: quiet period, max replies per post, max replies per tick, max context chars.' 'critical' 'TMR' 'EternalX incident'),
    (New-Tr 'TR-AI-ASYNC-001' 'Non-blocking AI work' 'AI and background generation must not block HTTP pipeline.' 'medium' 'ASYNC' 'Local'),
    (New-Tr 'TR-CORE-DATA-001' 'LiteDB persistence conventions' 'Single-file LiteDB, UTC times, LITEDB_PATH, posts/replies/users/votes/moderation/figures/peer groups/settings. Seed insert-if-absent.' 'critical' 'DATA' "$src; Reddit TR-CORE-DATA-001"),
    (New-Tr 'TR-CORE-CONC-001' 'Post write serialization' 'Whole-document writes serialized or re-fetch-append-save; AI generation outside lock.' 'high' 'CONC' "$src; Reddit TR-CORE-CONC-001"),
    (New-Tr 'TR-CORE-SEC-001' 'Gateway identity trust boundary' 'Accept X-Auth-* only when GATEWAY_KEY matches X-Gateway-Key; else anonymous.' 'high' 'SEC' "$src; Reddit TR-CORE-SEC-001"),
    (New-Tr 'TR-CORE-SEC-002' 'Forwarded headers' 'Clear KnownIPNetworks/KnownProxies; honor gateway proto/host; PATH_BASE absorption.' 'high' 'SEC' "$src; Reddit TR-CORE-SEC-002"),
    (New-Tr 'TR-CORE-SEC-003' 'No public site ports in estate mode' 'In gateway estate deploys eternalx exposes no public host ports; only gateway is public.' 'high' 'SEC' "$src; Reddit TR-CORE-SEC-003"),
    (New-Tr 'TR-CORE-VOL-001' 'Named volume eternalx-data' 'Persist under /app/data on eternalx-data volume.' 'high' 'VOL' "$src; Reddit TR-CORE-VOL-001"),
    (New-Tr 'TR-CORE-OCTO-001' 'Deploy script CWD contract' 'octopus-deploy.ps1 resolves repo root when CWD is script folder; git stderr via cmd /c under Windows PS 5.1.' 'high' 'OCTO' "$src; Reddit TR-CORE-OCTO-001"),
    (New-Tr 'TR-CORE-OCTO-002' 'Git trigger wiring' 'Per-project Git triggers and lifecycle for independent EternalX deploys.' 'medium' 'OCTO' "$src; Reddit TR-CORE-OCTO-002"),
    (New-Tr 'TR-CORE-OIDC-001' 'Standalone multi-provider OIDC' 'Configure Google, Microsoft, GitHub OIDC from env when not gateway-only.' 'critical' 'OIDC' 'Local override'),
    (New-Tr 'TR-CORE-GW-001' 'Gateway auth handler' 'When GATEWAY_KEY set, map headers only on key match.' 'high' 'GW' 'Local/imported'),
    (New-Tr 'TR-CORE-PAPI-001' 'Post create API' 'Authenticated post create with rate limit before side effects; persist LiteDB.' 'critical' 'PAPI' 'Local'),
    (New-Tr 'TR-CORE-RATE-001' 'Sliding window rate limit' 'One post per IP per minute.' 'critical' 'RATE' 'Local'),
    (New-Tr 'TR-CORE-VOTE-001' 'Vote persistence' 'Per-user vote dedupe and net scores; reject anonymous votes.' 'high' 'VOTE' 'Local'),
    (New-Tr 'TR-CORE-SHARE-001' 'Share link generation' 'Stable deep links to post/reply.' 'medium' 'SHARE' 'Local'),
    (New-Tr 'TR-CORE-ENV-001' 'Environment-only secrets' 'AI/OAuth secrets only from environment.' 'critical' 'ENV' 'Local'),
    (New-Tr 'TR-CORE-HLTH-001' 'Health endpoint' '/health 200 when process and DB reachable.' 'high' 'HLTH' 'Local'),
    (New-Tr 'TR-CORE-LOG-001' 'Structured event logging' 'Logs for post, AI, moderation, bans, background.' 'high' 'LOG' 'Local'),
    (New-Tr 'TR-CORE-DOCKER-001' 'Multi-stage container' 'Image on 8080, non-root, healthcheck, /app/data volume.' 'high' 'DOCKER' 'Local'),
    (New-Tr 'TR-CORE-PATH-001' 'PathBase' 'PATH_BASE=/x support with base href.' 'high' 'PATH' 'Local'),
    (New-Tr 'TR-CORE-API-001' 'Client data APIs' 'Feed read, post, vote, share, health for Blazor client; no client AI keys.' 'high' 'API' 'Local'),
    (New-Tr 'TR-CORE-OWN-001' 'Unique implementation ownership' 'No sibling src/tests as compile/copy inputs.' 'critical' 'OWN' 'Local'),
    (New-Tr 'TR-CORE-EXPORT-001' 'Export bundle contract' 'Versioned export/restore for EternalX entities; reject unsupported versions.' 'medium' 'EXPORT' "$src; Reddit TR-CORE-EXPORT-001")
)

$tests = @(
    (New-Test 'TEST-AI-001' 'Roster and generation unit coverage' 'Unit tests for figure picks, peer-group fallback, persona into prompts, provider/model metadata, optional scripted gag under concurrent writes with mocked providers.' 'high' "$src; Reddit TEST-AI-001"),
    (New-Test 'TEST-AI-002' 'Deep thread and moderation path' 'Mocked AI: bounded 5-7 replies after post; each moderated; injection/NSFW paths verified.' 'critical' 'Local'),
    (New-Test 'TEST-AI-003' 'AutoReply policy gates' 'AutoReplyPolicy: quiet period, max replies, tick limit, prompt truncation; simulated ticks cannot exceed cap.' 'critical' 'EternalX incident'),
    (New-Test 'TEST-AI-004' 'Server-side AI isolation' 'AI not callable from client; DEFAULT_AI_PROVIDER with mocks.' 'critical' 'Local'),
    (New-Test 'TEST-CORE-001' 'Data store and auth matrix' 'LiteDB round-trips, gateway header trust, OIDC config, vote/share authz, rate limit, concurrency no lost updates.' 'critical' "$src; Reddit TEST-CORE-001 adapted"),
    (New-Test 'TEST-CORE-002' 'PathBase and health' 'PATH_BASE and forwarded headers; /health behavior.' 'high' 'Local'),
    (New-Test 'TEST-CORE-003' 'Deploy script and docker contract' 'octopus-deploy.ps1 parse/CWD; Dockerfile 8080 healthcheck metadata.' 'medium' "$src; Reddit TEST-CORE-003 adapted"),
    (New-Test 'TEST-CORE-004' 'Seed and export' 'Idempotent seed; export/restore version rejection when implemented.' 'medium' "$src; Reddit TEST-CORE-004 adapted")
)

Split-Upsert -Records $frs -Existing $existingFr -CreateMethod 'workflow.requirements.createFrBatch' -UpdateMethod 'workflow.requirements.updateFrBatch'
Split-Upsert -Records $trs -Existing $existingTr -CreateMethod 'workflow.requirements.createTrBatch' -UpdateMethod 'workflow.requirements.updateTrBatch'
Split-Upsert -Records $tests -Existing $existingTest -CreateMethod 'workflow.requirements.createTestBatch' -UpdateMethod 'workflow.requirements.updateTestBatch'

$keepFr = @($frs | ForEach-Object { $_.id }) + @('FR-UI-001', 'FR-UI-002')
$keepTr = @($trs | ForEach-Object { $_.id }) + @('TR-UI-XFEED-001')
$keepTest = @($tests | ForEach-Object { $_.id }) + @('TEST-UI-001')

foreach ($id in (Get-IdsFromList 'workflow.requirements.listFr')) {
    if ($keepFr -notcontains $id) {
        Write-Host "deleteFr $id"
        Invoke-Mcp 'workflow.requirements.deleteFr' @{ id = $id } | Out-Null
    }
}
foreach ($id in (Get-IdsFromList 'workflow.requirements.listTr')) {
    if ($keepTr -notcontains $id) {
        Write-Host "deleteTr $id"
        Invoke-Mcp 'workflow.requirements.deleteTr' @{ id = $id } | Out-Null
    }
}
foreach ($id in (Get-IdsFromList 'workflow.requirements.listTest')) {
    if ($keepTest -notcontains $id) {
        Write-Host "deleteTest $id"
        Invoke-Mcp 'workflow.requirements.deleteTest' @{ id = $id } | Out-Null
    }
}

$maps = @(
    @{ fr = 'FR-AI-001'; tr = @('TR-AI-PROV-001', 'TR-AI-SEED-001'); test = @('TEST-AI-001', 'TEST-AI-004') },
    @{ fr = 'FR-AI-002'; tr = @('TR-AI-TMR-001', 'TR-AI-ASYNC-001'); test = @('TEST-AI-003') },
    @{ fr = 'FR-AI-003'; tr = @('TR-AI-GEN-001', 'TR-AI-THREAD-001', 'TR-AI-PROV-001'); test = @('TEST-AI-002', 'TEST-AI-001') },
    @{ fr = 'FR-AI-004'; tr = @('TR-AI-SEED-001', 'TR-AI-GEN-001'); test = @('TEST-AI-001') },
    @{ fr = 'FR-AI-005'; tr = @('TR-AI-SEED-001'); test = @('TEST-AI-001') },
    @{ fr = 'FR-AI-006'; tr = @('TR-AI-GEN-001'); test = @('TEST-AI-001', 'TEST-AI-002') },
    @{ fr = 'FR-AI-007'; tr = @('TR-AI-SEED-001', 'TR-AI-GEN-001'); test = @('TEST-AI-001') },
    @{ fr = 'FR-AI-008'; tr = @('TR-AI-SEED-001'); test = @('TEST-AI-001') },
    @{ fr = 'FR-AI-009'; tr = @('TR-AI-SEED-001', 'TR-AI-GEN-001'); test = @('TEST-AI-001') },
    @{ fr = 'FR-AI-010'; tr = @('TR-AI-SCAN-001'); test = @('TEST-AI-002') },
    @{ fr = 'FR-AI-011'; tr = @('TR-AI-SCAN-001'); test = @('TEST-AI-002') },
    @{ fr = 'FR-AI-012'; tr = @('TR-AI-SCAN-001', 'TR-CORE-LOG-001'); test = @('TEST-AI-002') },
    @{ fr = 'FR-AI-013'; tr = @('TR-AI-TMR-001', 'TR-AI-GEN-001'); test = @('TEST-AI-003') },
    @{ fr = 'FR-CORE-001'; tr = @('TR-CORE-DATA-001', 'TR-CORE-API-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-002'; tr = @('TR-CORE-DATA-001', 'TR-AI-SEED-001'); test = @('TEST-AI-001', 'TEST-CORE-001') },
    @{ fr = 'FR-CORE-003'; tr = @('TR-CORE-PAPI-001', 'TR-CORE-CONC-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-004'; tr = @('TR-CORE-SEC-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-005'; tr = @('TR-CORE-EXPORT-001', 'TR-CORE-DATA-001'); test = @('TEST-CORE-004') },
    @{ fr = 'FR-CORE-006'; tr = @('TR-CORE-CONC-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-007'; tr = @('TR-CORE-VOTE-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-008'; tr = @('TR-CORE-SHARE-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-009'; tr = @('TR-CORE-SEC-003', 'TR-CORE-PATH-001'); test = @('TEST-CORE-002', 'TEST-CORE-003') },
    @{ fr = 'FR-CORE-010'; tr = @('TR-CORE-SEC-001', 'TR-CORE-GW-001', 'TR-CORE-OIDC-001'); test = @('TEST-CORE-001', 'TEST-CORE-002') },
    @{ fr = 'FR-CORE-011'; tr = @('TR-CORE-SEC-002', 'TR-CORE-GW-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-012'; tr = @('TR-CORE-OCTO-001', 'TR-CORE-OCTO-002'); test = @('TEST-CORE-003') },
    @{ fr = 'FR-CORE-013'; tr = @('TR-CORE-OCTO-002'); test = @('TEST-CORE-003') },
    @{ fr = 'FR-CORE-014'; tr = @('TR-CORE-DATA-001'); test = @('TEST-CORE-001', 'TEST-CORE-004') },
    @{ fr = 'FR-CORE-015'; tr = @('TR-CORE-VOL-001', 'TR-CORE-DOCKER-001'); test = @('TEST-CORE-003') },
    @{ fr = 'FR-CORE-016'; tr = @('TR-AI-SEED-001', 'TR-CORE-DATA-001'); test = @('TEST-CORE-004') },
    @{ fr = 'FR-CORE-017'; tr = @('TR-CORE-EXPORT-001'); test = @('TEST-CORE-004') },
    @{ fr = 'FR-CORE-018'; tr = @('TR-CORE-OWN-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-019'; tr = @('TR-CORE-RATE-001'); test = @('TEST-CORE-001') },
    @{ fr = 'FR-CORE-020'; tr = @('TR-CORE-HLTH-001', 'TR-CORE-LOG-001'); test = @('TEST-CORE-002') },
    @{ fr = 'FR-CORE-021'; tr = @('TR-CORE-DOCKER-001'); test = @('TEST-CORE-003') },
    @{ fr = 'FR-CORE-022'; tr = @('TR-CORE-PATH-001', 'TR-CORE-SEC-002'); test = @('TEST-CORE-002') },
    @{ fr = 'FR-CORE-023'; tr = @('TR-CORE-ENV-001'); test = @('TEST-AI-004') },
    @{ fr = 'FR-UI-001'; tr = @('TR-UI-XFEED-001'); test = @('TEST-UI-001') },
    @{ fr = 'FR-UI-002'; tr = @('TR-UI-XFEED-001', 'TR-CORE-API-001'); test = @('TEST-UI-001', 'TEST-CORE-001') }
)

$mapOk = 0
$mapFail = 0
foreach ($m in $maps) {
    $trYaml = ($m.tr | ForEach-Object { "  - $_" }) -join "`n"
    $testYaml = ($m.test | ForEach-Object { "  - $_" }) -join "`n"
    $yaml = "frId: $($m.fr)`ntrIds:`n$trYaml`ntestIds:`n$testYaml`nnotes: AI/CORE import adapted from EternalReddit wiki + local EternalX"
    $out = & $plugin -Command Invoke -Method 'workflow.requirements.createMapping' -Params $yaml -WorkspacePath $ws -TimeoutSeconds 60 2>&1 | Out-String
    if ($out -match 'type: result' -and $out -notmatch 'type: error') {
        $mapOk++
    }
    elseif ($out -match 'already|exists|duplicate') {
        $mapOk++
    }
    else {
        $mapFail++
        Write-Host "map issue $($m.fr)"
    }
}
Write-Host "maps ok=$mapOk fail=$mapFail"

Write-Host '=== FINAL FR by area ==='
$out = & $plugin -Command Invoke -Method 'workflow.requirements.listFr' -Params '' -WorkspacePath $ws -TimeoutSeconds 90 | Out-String
$id = $null
$areas = @{}
foreach ($line in ($out -split "`n")) {
    if ($line -match '^\s+- id:\s*(\S+)') { $id = $Matches[1] }
    elseif ($id -and $line -match '^\s+area:\s*(\S+)') {
        $a = $Matches[1]
        if (-not $areas.ContainsKey($a)) { $areas[$a] = [System.Collections.Generic.List[string]]::new() }
        [void]$areas[$a].Add($id)
        $id = $null
    }
}
foreach ($k in ($areas.Keys | Sort-Object)) {
    Write-Host "$k : $($areas[$k].Count)"
    $areas[$k] | Sort-Object | ForEach-Object { Write-Host "  $_" }
}
Write-Host (($out | Select-String 'totalCount:').Line)

Write-Host '=== TR count ==='
$trOut = & $plugin -Command Invoke -Method 'workflow.requirements.listTr' -Params '' -WorkspacePath $ws -TimeoutSeconds 90 | Out-String
Write-Host (($trOut | Select-String 'totalCount:').Line)
Write-Host '=== TEST count ==='
$teOut = & $plugin -Command Invoke -Method 'workflow.requirements.listTest' -Params '' -WorkspacePath $ws -TimeoutSeconds 90 | Out-String
Write-Host (($teOut | Select-String 'totalCount:').Line)

Invoke-Mcp 'workflow.requirements.generateDocument' @{ format = 'wiki'; docType = 'all' } | Out-Null
Write-Host 'wiki ok'
Write-Host "timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')"
