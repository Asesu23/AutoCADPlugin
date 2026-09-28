param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Message
)

Set-Location $PSScriptRoot

function Fail($Text) {
    Write-Host $Text -ForegroundColor Red
    exit 1
}

function Invoke-Git {
    & git @args
    if ($LASTEXITCODE -ne 0) {
        Fail "git $($args -join ' ') failed"
    }
}

$Version = $Version.TrimStart('v')
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    Fail 'Version must look like 4.1.0'
}
$tag = "v$Version"

$branch = (& git rev-parse --abbrev-ref HEAD)
if ($branch -ne 'master') {
    Fail "Switch to master first (current branch: $branch)"
}

if (& git tag --list $tag) {
    Fail "Tag $tag already exists locally"
}
$remoteTag = & git ls-remote --tags origin "refs/tags/$tag"
if ($LASTEXITCODE -ne 0) {
    Fail 'Could not reach origin'
}
if ($remoteTag) {
    Fail "Tag $tag already exists on origin"
}

if (& git status --porcelain) {
    if (-not $Message) {
        Fail "Uncommitted changes found. Pass a commit message: .\release.ps1 $Version `"feat: ...`""
    }
    Invoke-Git add -A
    Invoke-Git commit --quiet -m $Message
}

Invoke-Git push --quiet origin master
Invoke-Git tag $tag
Invoke-Git push --quiet origin $tag

Write-Host "Released $tag" -ForegroundColor Green
Write-Host 'Build status: https://github.com/Asesu23/Jpegger/actions'
