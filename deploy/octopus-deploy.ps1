# EternalX deploy: built and run independently; a push to this repo triggers it
# (Octopus Git trigger polls main and the Development phase auto-deploys the release).
# Joins the shared eternal docker network behind the EternalSocial gateway at /x
# and authenticates users via the stable GATEWAY_KEY (EternalSocial library set).
#
# AI keys reuse the same Octopus Sensitive names as EternalReddit / EternalReadit
# (library or project variables already provisioned for Claude + Grok):
#   ANTHROPIC_API_KEY, XAI_API_KEY, OPENAI_API_KEY, HF_API_KEY
# Optional aliases also accepted by the app if present in Octopus:
#   CLAUDE_API_KEY, GROK_API_KEY, HUGGINGFACE_API_KEY
$ErrorActionPreference = 'Stop'

$image = 'eternalx:latest'
$container = 'eternalx'
$network = 'eternal'
$sub = [string][char]114 + [char]109

function TeardownContainer($name) {
    $ex = docker ps -aq --filter "name=^/$name$"
    if ($ex) {
        $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        & docker stop $name 2>&1 | Out-Null
        & docker $sub '-f' $name 2>&1 | Out-Null
        $ErrorActionPreference = $eap
        $global:LASTEXITCODE = 0
    }
}

$gatewayKey = $OctopusParameters['GATEWAY_KEY']
if (-not $gatewayKey) { throw 'GATEWAY_KEY variable is not set (EternalSocial library set).' }

# Git-sourced steps extract the repo one level ABOVE this script's folder and run the
# script with CWD = the script's folder, so probe $PSScriptRoot's parent, then $PWD.
$src = Split-Path -Parent $PSScriptRoot
if (-not ($src -and (Test-Path (Join-Path $src 'Dockerfile')))) { $src = "$PWD" }
if (-not (Test-Path (Join-Path $src 'Dockerfile'))) {
    # Ad-hoc fallback: clone/refresh a working copy. git writes progress to stderr;
    # cmd /c merges the streams outside PowerShell so EAP=Stop cannot treat it as fatal.
    $work = Join-Path $env:ProgramData 'EternalX\src'
    New-Item -ItemType Directory -Force (Split-Path $work) | Out-Null
    if (Test-Path (Join-Path $work '.git')) {
        cmd /c "git -C ""$work"" fetch --all --prune 2>&1" | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "git fetch failed with exit code $LASTEXITCODE" }
        cmd /c "git -C ""$work"" reset --hard origin/main 2>&1" | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "git reset failed with exit code $LASTEXITCODE" }
    } else {
        cmd /c "git clone --branch main --depth 1 https://github.com/sharpninja/EternalX.Blazor.git ""$work"" 2>&1" | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "git clone failed with exit code $LASTEXITCODE" }
    }
    $src = $work
}

docker build -t $image "$src"
if ($LASTEXITCODE -ne 0) { throw "docker build (eternalx) failed with exit code $LASTEXITCODE" }

if (-not (docker network ls -q --filter "name=^$network$")) {
    docker network create $network | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "docker network create failed with exit code $LASTEXITCODE" }
}

# Shared AI Sensitive variables (same names as EternalReadit) plus optional aliases / settings.
# Only non-empty values are written so missing optional keys do not blank the container.
$envFile = Join-Path $env:TEMP 'eternalx.env'
$names = @(
    'ANTHROPIC_API_KEY',
    'CLAUDE_API_KEY',
    'OPENAI_API_KEY',
    'XAI_API_KEY',
    'GROK_API_KEY',
    'HF_API_KEY',
    'HUGGINGFACE_API_KEY',
    'DEFAULT_AI_PROVIDER',
    'ANTHROPIC_MODEL',
    'CLAUDE_MODEL',
    'OPENAI_MODEL',
    'XAI_MODEL',
    'GROK_MODEL',
    'Authorization__AdminEmail'
)
$lines = foreach ($n in $names) {
    $v = $OctopusParameters[$n]
    if ($v) { "$n=$v" }
}
$lines = @($lines) + "GATEWAY_KEY=$gatewayKey"
[System.IO.File]::WriteAllLines($envFile, [string[]]$lines)

$liveClaude = [bool]($OctopusParameters['ANTHROPIC_API_KEY'] -or $OctopusParameters['CLAUDE_API_KEY'])
$liveGrok = [bool]($OctopusParameters['XAI_API_KEY'] -or $OctopusParameters['GROK_API_KEY'])
Write-Host ("AI keys present for container: claude={0} grok={1} openai={2} hf={3}" -f `
    $liveClaude, `
    $liveGrok, `
    [bool]$OctopusParameters['OPENAI_API_KEY'], `
    [bool]($OctopusParameters['HF_API_KEY'] -or $OctopusParameters['HUGGINGFACE_API_KEY']))

try {
    TeardownContainer $container
    docker run -d --name $container --restart unless-stopped --network $network `
        -v eternalx-data:/app/data `
        -e ASPNETCORE_ENVIRONMENT=Production `
        -e PATH_BASE=/x `
        --env-file $envFile `
        $image
    if ($LASTEXITCODE -ne 0) { throw "docker run (eternalx) failed with exit code $LASTEXITCODE" }
}
finally {
    try { [System.IO.File]::Delete($envFile) } catch { }
}

Write-Host 'EternalX deployed behind the gateway at /x (live AI when Claude/Grok keys are in Octopus).'
