param (
    [string]$SolutionDir = $PSScriptRoot,
    [string]$OutputFile = (Join-Path $PSScriptRoot "GitContentExport.zip")
)

$SolutionDir = (Get-Item $SolutionDir).FullName
Set-Location $SolutionDir

# Verify git is available and directory is a git repository
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "Git is not installed or not in PATH."
    return
}

if (-not (git rev-parse --is-inside-work-tree 2>$null)) {
    Write-Error "The directory '$SolutionDir' is not a Git repository."
    return
}

# --- Exclude Output File from Git ---
$gitignorePath = Join-Path $SolutionDir ".gitignore"
# Calculate path relative to root directory for cleaner .gitignore entry
$baseUri = New-Object System.Uri(($SolutionDir.TrimEnd('\') + '\'))
$targetUri = New-Object System.Uri($OutputFile)
$relativeOutputFile = [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString().Replace('\', '/'))

$entryExists = $false
if (Test-Path $gitignorePath) {
    $gitignoreContent = Get-Content $gitignorePath
    if ($gitignoreContent -contains $relativeOutputFile -or $gitignoreContent -contains [System.IO.Path]::GetFileName($OutputFile)) {
        $entryExists = $true
    }
}

if (-not $entryExists) {
    Add-Content -Path $gitignorePath -Value "`n$relativeOutputFile"
    Write-Host "Added '$relativeOutputFile' to .gitignore"
}

# --- Prepare Output Path ---
$outDir = [System.IO.Path]::GetDirectoryName($OutputFile)
if (-not (Test-Path $outDir)) { 
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null 
}

# Remove existing output file prior to export
if (Test-Path $OutputFile) { 
    Remove-Item $OutputFile -Force 
}

# Export all tracked files to ZIP
# Export tracked files, including those inside submodules
Add-Type -AssemblyName System.IO.Compression.FileSystem

$files = git -c core.quotepath=off ls-files --recurse-submodules

$zip = [System.IO.Compression.ZipFile]::Open($OutputFile, 'Create')
try {
    foreach ($f in $files) {
        $full = Join-Path $SolutionDir $f
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { continue }
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $full, $f, 'Optimal')
    }
}
finally {
    $zip.Dispose()
}

Write-Host "Source export successfully created at: $OutputFile"