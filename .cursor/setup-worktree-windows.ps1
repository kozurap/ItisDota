$ErrorActionPreference = 'Stop'

$worktreeRoot = Split-Path -Parent $PSScriptRoot
Set-Location $worktreeRoot

Write-Host "Restoring ItisDota.sln in $worktreeRoot"
dotnet restore ItisDota.sln

$mainRoot = $env:ROOT_WORKTREE_PATH
if ([string]::IsNullOrWhiteSpace($mainRoot) -or -not (Test-Path $mainRoot)) {
    Write-Host "ROOT_WORKTREE_PATH is not set; skipped copy of local untracked files."
    Write-Host "Worktree setup complete."
    exit 0
}

$mainRoot = (Resolve-Path $mainRoot).Path.TrimEnd('\')
$copied = 0

foreach ($name in @('.env', 'secrets.json')) {
    $source = Join-Path $mainRoot $name
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $worktreeRoot $name) -Force
        Write-Host "Copied $name"
        $copied++
    }
}

Get-ChildItem -LiteralPath $mainRoot -Filter 'appsettings.*.local.json' -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    ForEach-Object {
        $relative = $_.FullName.Substring($mainRoot.Length).TrimStart('\')
        $destination = Join-Path $worktreeRoot $relative
        $destinationDir = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $destinationDir)) {
            New-Item -ItemType Directory -Path $destinationDir | Out-Null
        }

        Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        Write-Host "Copied $relative"
        $copied++
    }

Write-Host "Copied $copied local file(s). Worktree setup complete."
