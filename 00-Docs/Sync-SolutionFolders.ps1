# Sync-SolutionFolders.ps1
# Rebuilds nested Visual Studio solution folders in MLNetMasterSuite.sln
# to mirror the on-disk lab folder structure. Re-run after adding new projects.
#
# Usage (from repo root):
#   ./00-Docs/Sync-SolutionFolders.ps1

param(
    [string]$SolutionPath = (Join-Path $PSScriptRoot "..\MLNetMasterSuite.sln"),
    [string]$RepoRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"

$SolutionFolderTypeGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8"
$ExcludeDirNames = @(".git", ".cursor", "bin", "obj", "Properties", "data")
$ExcludeTopLevel = @(".git", ".cursor", ".github")
$ProjectSuffixPattern = "\.(Training|Api|Tests)$"

function Get-StableGuid([string]$Seed) {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("MLNetLab:$Seed"))
    return [guid]::New($bytes).ToString().ToUpperInvariant()
}

function ShouldIncludeDirectory([string]$Name, [string]$ParentRelativePath) {
    if ($Name -in $ExcludeDirNames) { return $false }
    if ($Name -eq "model" -and $ParentRelativePath -ne "") { return $false }
    if ($Name -match $ProjectSuffixPattern) { return $false }
    return $true
}

function Get-SolutionFolderPaths([string]$Root) {
    $folders = [System.Collections.Generic.List[string]]::new()

    function Walk([string]$AbsolutePath, [string]$RelativePath) {
        if ($RelativePath -ne "") {
            $folders.Add($RelativePath)
        }

        Get-ChildItem -LiteralPath $AbsolutePath -Directory -ErrorAction SilentlyContinue |
            ForEach-Object {
                if (-not (ShouldIncludeDirectory $_.Name $RelativePath)) { return }
                $childRelative = if ($RelativePath) { "$RelativePath\$($_.Name)" } else { $_.Name }
                Walk $_.FullName $childRelative
            }
    }

    Get-ChildItem -LiteralPath $Root -Directory |
        Where-Object { $_.Name -notin $ExcludeTopLevel } |
        ForEach-Object { Walk $_.FullName $_.Name }

    return $folders | Sort-Object
}

function Parse-SolutionProjects([string[]]$Lines) {
    $projects = @()
    $i = 0
    while ($i -lt $Lines.Count) {
        $line = $Lines[$i]
        if ($line -match '^Project\("\{([0-9A-Fa-f-]+)\}"\) = "([^"]+)", "([^"]*)", "\{([0-9A-Fa-f-]+)\}"$') {
            $typeGuid = $Matches[1].ToUpperInvariant()
            $name = $Matches[2]
            $path = $Matches[3]
            $guid = $Matches[4].ToUpperInvariant()

            $entry = [ordered]@{
                TypeGuid = $typeGuid
                Name     = $name
                Path     = $path
                Guid     = $guid
                Lines    = @($line)
            }

            if ($typeGuid -eq $SolutionFolderTypeGuid.ToUpperInvariant()) {
                $i++
                while ($i -lt $Lines.Count -and $Lines[$i] -ne "EndProject") {
                    $entry.Lines += $Lines[$i]
                    $i++
                }
                if ($i -lt $Lines.Count) { $entry.Lines += $Lines[$i] }
            }
            else {
                $i++
                while ($i -lt $Lines.Count -and $Lines[$i] -ne "EndProject") {
                    $entry.Lines += $Lines[$i]
                    $i++
                }
                if ($i -lt $Lines.Count) { $entry.Lines += $Lines[$i] }
            }

            if ($typeGuid -ne $SolutionFolderTypeGuid.ToUpperInvariant()) {
                $projects += [pscustomobject]$entry
            }
        }
        $i++
    }
    return $projects
}

function Get-GlobalSections([string[]]$Lines) {
    $sections = @{}
    $inGlobal = $false
    $current = $null
    $buffer = @()

    foreach ($line in $Lines) {
        if ($line -eq "Global") { $inGlobal = $true; continue }
        if ($line -eq "EndGlobal" -and $inGlobal) {
            if ($current) { $sections[$current] = $buffer }
            break
        }
        if (-not $inGlobal) { continue }

        if ($line -match '^\tGlobalSection\(([^)]+)\) = (preSolution|postSolution)$') {
            if ($current) { $sections[$current] = $buffer }
            $current = $Matches[1]
            $buffer = @()
            continue
        }

        if ($line -eq "`tEndGlobalSection") {
            if ($current) { $sections[$current] = $buffer }
            $current = $null
            $buffer = @()
            continue
        }

        if ($null -ne $current) { $buffer += $line }
    }

    return $sections
}

function Get-ProjectFolderRelativePath([string]$ProjectPath) {
    $dir = Split-Path $ProjectPath -Parent
    $leaf = Split-Path $dir -Leaf
    if ($leaf -match $ProjectSuffixPattern) {
        return (Split-Path $dir -Parent) -replace "/", "\"
    }
    return $dir -replace "/", "\"
}

function Get-DocSolutionItems([string]$DocsPath) {
    if (-not (Test-Path $DocsPath)) { return @() }
    Get-ChildItem -LiteralPath $DocsPath -File |
        Where-Object { $_.Extension -in @(".md", ".txt", ".ps1") -and $_.Name -ne "Sync-SolutionFolders.ps1" } |
        ForEach-Object {
            [pscustomobject]@{
                Name = $_.Name
                Path = "00-Docs\$($_.Name)"
            }
        }
}

function Test-LabProjectFolder([string]$AbsolutePath) {
    Test-Path -LiteralPath (Join-Path $AbsolutePath "data") -PathType Container
}

function Ensure-ProjectAssetDirectories([string]$Root, [string[]]$FolderPaths) {
    foreach ($path in $FolderPaths) {
        $absolute = Join-Path $Root $path
        if (-not (Test-LabProjectFolder $absolute)) { continue }

        $modelAbsolute = Join-Path $absolute "model"
        if (-not (Test-Path -LiteralPath $modelAbsolute)) {
            New-Item -ItemType Directory -Path $modelAbsolute -Force | Out-Null
        }

        $gitkeep = Join-Path $modelAbsolute ".gitkeep"
        if (-not (Get-ChildItem -LiteralPath $modelAbsolute -File -ErrorAction SilentlyContinue)) {
            Set-Content -LiteralPath $gitkeep -Value "# Save trained ML.NET model .zip files here." -Encoding UTF8
        }
    }
}

function Add-AssetFolderPaths([string]$Root, [string[]]$FolderPaths) {
    $extended = [System.Collections.Generic.List[string]]::new()
    foreach ($path in $FolderPaths) { [void]$extended.Add($path) }

    foreach ($path in $FolderPaths) {
        $absolute = Join-Path $Root $path
        if (-not (Test-LabProjectFolder $absolute)) { continue }

        $dataPath = "$path\data"
        $modelPath = "$path\model"
        if ((Test-Path -LiteralPath (Join-Path $Root $dataPath) -PathType Container) -and -not $extended.Contains($dataPath)) {
            [void]$extended.Add($dataPath)
        }
        if (-not $extended.Contains($modelPath)) {
            [void]$extended.Add($modelPath)
        }
    }

    return @($extended | Sort-Object -Unique)
}

function Get-DataFolderSolutionItems([string]$Root, [string]$FolderRelativePath) {
    $absolute = Join-Path $Root $FolderRelativePath
    if (-not (Test-Path -LiteralPath $absolute)) { return @() }

    Get-ChildItem -LiteralPath $absolute -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @(".csv", ".json", ".txt", ".tsv") } |
        ForEach-Object {
            [pscustomobject]@{
                Name = $_.Name
                Path = "$FolderRelativePath\$($_.Name)"
            }
        }
}

function Get-ModelFolderSolutionItems([string]$Root, [string]$FolderRelativePath) {
    $absolute = Join-Path $Root $FolderRelativePath
    if (-not (Test-Path -LiteralPath $absolute)) { return @() }

    Get-ChildItem -LiteralPath $absolute -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($absolute.Length).TrimStart('\')
            [pscustomobject]@{
                Name = $relativePath
                Path = "$FolderRelativePath\$relativePath"
            }
        }
}

function Get-RootModelSolutionItems([string]$Root) {
    $absolute = Join-Path $Root "model"
    if (-not (Test-Path -LiteralPath $absolute)) { return @() }

    Get-ChildItem -LiteralPath $absolute -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($absolute.Length).TrimStart('\')
            [pscustomobject]@{
                Name = $relativePath
                Path = "model\$relativePath"
            }
        }
}

function Write-SolutionFolderEntry(
    [System.Collections.Generic.List[string]]$Output,
    [string]$Name,
    [string]$VirtualPath,
    [string]$Guid,
    [array]$SolutionItems
) {
    $Output.Add("Project(`"{$SolutionFolderTypeGuid}`") = `"$Name`", `"$VirtualPath`", `"{$Guid}`"")
    $items = @($SolutionItems)
    if ($items.Count -gt 0) {
        $Output.Add("`tProjectSection(SolutionItems) = preProject")
        foreach ($item in ($items | Sort-Object Path)) {
            $Output.Add("`t`t$($item.Name) = $($item.Path)")
        }
        $Output.Add("`tEndProjectSection")
    }
    $Output.Add("EndProject")
}

$SolutionPath = (Resolve-Path $SolutionPath).Path
$RepoRoot = (Resolve-Path $RepoRoot).Path

if (-not (Test-Path $SolutionPath)) {
    throw "Solution file not found: $SolutionPath"
}

$originalLines = Get-Content -LiteralPath $SolutionPath -Encoding UTF8
$codeProjects = Parse-SolutionProjects $originalLines
$globalSections = Get-GlobalSections $originalLines
$folderPaths = @(Get-SolutionFolderPaths $RepoRoot)
Ensure-ProjectAssetDirectories $RepoRoot $folderPaths
$folderPaths = Add-AssetFolderPaths $RepoRoot $folderPaths

$folderGuids = @{}
foreach ($folder in $folderPaths) {
    $folderGuids[$folder] = Get-StableGuid "folder:$folder"
}

$nested = [System.Collections.Generic.List[string]]::new()

foreach ($folder in $folderPaths) {
    $parent = Split-Path $folder -Parent
    if ($parent) {
        $nested.Add("`t`t{$($folderGuids[$folder])} = {$($folderGuids[$parent])}")
    }
}

foreach ($project in $codeProjects) {
    $parentPath = Get-ProjectFolderRelativePath $project.Path
    if ($folderGuids.ContainsKey($parentPath)) {
        $nested.Add("`t`t{$($project.Guid)} = {$($folderGuids[$parentPath])}")
    }
}

$docsItems = @(Get-DocSolutionItems (Join-Path $RepoRoot "00-Docs"))
$docsGuid = $folderGuids["00-Docs"]

$output = [System.Collections.Generic.List[string]]::new()
$output.Add("")
$output.Add("Microsoft Visual Studio Solution File, Format Version 12.00")
$output.Add("# Visual Studio Version 17")
$output.Add("VisualStudioVersion = 17.0.31903.59")
$output.Add("MinimumVisualStudioVersion = 10.0.40219.1")

foreach ($folder in $folderPaths) {
    $name = Split-Path $folder -Leaf
    $guid = $folderGuids[$folder]
    $solutionItems = @()

    if ($folder -eq "00-Docs") {
        $solutionItems = $docsItems
    }
    elseif ($folder -eq "model") {
        $solutionItems = Get-RootModelSolutionItems $RepoRoot
    }
    elseif ($name -eq "data") {
        $solutionItems = Get-DataFolderSolutionItems $RepoRoot $folder
    }
    elseif ($name -eq "model") {
        $solutionItems = Get-ModelFolderSolutionItems $RepoRoot $folder
    }

    Write-SolutionFolderEntry -Output $output -Name $name -VirtualPath $folder -Guid $guid -SolutionItems $solutionItems
}

foreach ($project in $codeProjects) {
    foreach ($line in $project.Lines) {
        $output.Add($line)
    }
}

$output.Add("Global")

if ($globalSections.ContainsKey("SolutionConfigurationPlatforms")) {
    $output.Add("`tGlobalSection(SolutionConfigurationPlatforms) = preSolution")
    foreach ($line in $globalSections["SolutionConfigurationPlatforms"]) { $output.Add($line) }
    $output.Add("`tEndGlobalSection")
}

if ($globalSections.ContainsKey("ProjectConfigurationPlatforms")) {
    $output.Add("`tGlobalSection(ProjectConfigurationPlatforms) = postSolution")
    foreach ($line in $globalSections["ProjectConfigurationPlatforms"]) { $output.Add($line) }
    $output.Add("`tEndGlobalSection")
}

$output.Add("`tGlobalSection(SolutionProperties) = preSolution")
if ($globalSections.ContainsKey("SolutionProperties")) {
    foreach ($line in $globalSections["SolutionProperties"]) { $output.Add($line) }
}
else {
    $output.Add("`t`tHideSolutionNode = FALSE")
}
$output.Add("`tEndGlobalSection")

$output.Add("`tGlobalSection(NestedProjects) = preSolution")
foreach ($line in ($nested | Sort-Object -Unique)) { $output.Add($line) }
$output.Add("`tEndGlobalSection")

$output.Add("EndGlobal")
$output.Add("")

[System.IO.File]::WriteAllLines($SolutionPath, $output, [System.Text.UTF8Encoding]::new($false))

$csvLinks = @($folderPaths | Where-Object { $_ -like "*\data" }).Count
$dataFolders = @($folderPaths | Where-Object { $_ -like "*\data" }).Count
$modelFolders = @($folderPaths | Where-Object { $_ -like "*\model" -or $_ -eq "model" }).Count
Write-Host "Updated solution folders: $($folderPaths.Count) (data: $dataFolders, model: $modelFolders)"
Write-Host "CSV dataset folders linked: $csvLinks"
Write-Host "Nested code projects: $($codeProjects.Count)"
Write-Host "Solution: $SolutionPath"
