# Post-build script to create multi-target NuGet package using dotnet pack
# This script automatically includes all available Framework DLLs

param(
    [string]$ProjectDir,
    [string]$Configuration = "Release",
	[string]$AssemblyName = "MbUnit",
    [string]$PackageId = "MbUnit.V4",
    [string]$Version = "4.0.0"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building Multi-Target NuGet Package" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Project: $AssemblyName" -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor Yellow

# Only run in Release mode
if ($Configuration -ne "Release") {
    Write-Host "Skipping package creation (not Release mode)" -ForegroundColor Gray
    exit 0
}

# Base paths
$srcRoot = Split-Path (Split-Path $ProjectDir -Parent) -Parent
$mbunitRoot = Join-Path $srcRoot "MbUnit"
$outputDir = Join-Path $ProjectDir "bin\$Configuration"

# Create a temporary packaging project
$tempProjDir = Join-Path $ProjectDir "obj\TempPackageProject"
$tempProjFile = Join-Path $tempProjDir "$AssemblyName.Temp.csproj"

if (Test-Path $tempProjDir) {
    Remove-Item $tempProjDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempProjDir -Force | Out-Null

Write-Host "`nSearching for Framework DLLs..." -ForegroundColor Cyan

# Define framework mappings
$frameworks = @(
    @{ Name = "net35"; Path = Join-Path $mbunitRoot "MbUnit35\bin" },
    @{ Name = "net40"; Path = Join-Path $mbunitRoot "MbUnit40\bin" },
    @{ Name = "net48"; Path = Join-Path $mbunitRoot "MbUnit\bin" },
    @{ Name = "net8.0-windows7.0"; Path = Join-Path $outputDir "net8.0-windows" }
)

# Build ItemGroup XML for all DLLs found
$itemGroups = ""
$foundCount = 0

foreach ($fw in $frameworks) {
    $fwPath = $fw.Path
    $dllPath = Join-Path $fwPath "$AssemblyName.dll"
    
    if (Test-Path $dllPath) {
        Write-Host "  v Found: $($fw.Name)" -ForegroundColor Green
        
        # Add DLL to package
        $itemGroups += "    <None Include=`"$dllPath`" Pack=`"true`" PackagePath=`"lib\$($fw.Name)\`" />`r`n"
        
        # Add PDB if exists
        $pdbPath = Join-Path $fwPath "$AssemblyName.pdb"
        if (Test-Path $pdbPath) {
            $itemGroups += "    <None Include=`"$pdbPath`" Pack=`"true`" PackagePath=`"lib\$($fw.Name)\`" />`r`n"
        }
        
        # Add XML if exists  
        $xmlPath = Join-Path $fwPath "$AssemblyName.xml"
        if (Test-Path $xmlPath) {
            $itemGroups += "    <None Include=`"$xmlPath`" Pack=`"true`" PackagePath=`"lib\$($fw.Name)\`" />`r`n"
        }
        
        $foundCount++
    } else {
        Write-Host "  x Missing: $($fw.Name)" -ForegroundColor Gray
    }
}

Write-Host "`nTotal frameworks found: $foundCount" -ForegroundColor Yellow

if ($foundCount -eq 0) {
    Write-Host "`nNo DLLs found! Skipping package creation." -ForegroundColor Yellow
    exit 0
}

# Paths for package assets
$readmePath = Join-Path $ProjectDir "README.md"
$iconPath = Join-Path $mbunitRoot "MbUnit\Resources\MbUnit.png"

# Create temporary packaging .csproj
$tempCsprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net35;net40;net48;net8.0-windows7.0</TargetFrameworks>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <NoBuild>true</NoBuild>
	<SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    
    <PackageId>$PackageId</PackageId>
    <Version>$Version</Version>
    <Authors>Gallio Project, Bart Suelze</Authors>
    <Description>$AssemblyName Test Framework v4 - Multi-target package supporting .NET Framework 3.5+ and .NET 8-windows.</Description>
    <Copyright>Copyright © 2005-2025 Gallio Project</Copyright>
    <PackageTags>testing;test-framework;mbunit;unit-testing;tdd;bdd;automation;dotnet;csharp</PackageTags>
    <PackageProjectUrl>https://github.com/soelske/mbunit-v3</PackageProjectUrl>
    <RepositoryUrl>https://github.com/soelske/mbunit-v3</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>MbUnit.png</PackageIcon>
  </PropertyGroup>
  
  <ItemGroup>
    <None Include="$readmePath" Pack="true" PackagePath="\" />
    <None Include="$iconPath" Pack="true" PackagePath="\" />
$itemGroups  </ItemGroup>
</Project>
"@

$tempCsprojContent | Out-File -FilePath $tempProjFile -Encoding UTF8

Write-Host "`nCreating NuGet package using dotnet pack..." -ForegroundColor Cyan

# Run dotnet pack (with restore to avoid asset errors)
$packResult = & dotnet pack $tempProjFile -o $outputDir -c Release 2>&1 | Out-String

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nv Package created successfully!" -ForegroundColor Green
    $packageFile = Join-Path $outputDir "$AssemblyName.$Version.nupkg"
    if (Test-Path $packageFile) {
        $pkgInfo = Get-Item $packageFile
        Write-Host "Package: $($pkgInfo.Name)" -ForegroundColor Cyan
        Write-Host "Size: $([math]::Round($pkgInfo.Length/1KB, 2)) KB" -ForegroundColor Cyan
        Write-Host "Location: $($pkgInfo.FullName)" -ForegroundColor Cyan
    }
} else {
    Write-Host "`nPackage creation had warnings/errors:" -ForegroundColor Yellow
    Write-Host $packResult
}

# Cleanup temp project
Remove-Item $tempProjDir -Recurse -Force -ErrorAction SilentlyContinue

# Rename
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nv Package created successfully!" -ForegroundColor Green
    
    $packageFile = Join-Path $outputDir "$PackageId.$Version.nupkg"
    $renamedFile = Join-Path $outputDir "$AssemblyName.$Version.nupkg"
    
    # Remove old renamed file if it already exists
    if (Test-Path $renamedFile) {
        Remove-Item $renamedFile -Force
    }
    
    if (Test-Path $packageFile) {
        Rename-Item -Path $packageFile -NewName "$AssemblyName.$Version.nupkg" -Force
        $pkgInfo = Get-Item $renamedFile
        Write-Host "Package: $($pkgInfo.Name)" -ForegroundColor Cyan
        Write-Host "Size: $([math]::Round($pkgInfo.Length/1KB, 2)) KB" -ForegroundColor Cyan
        Write-Host "Location: $($pkgInfo.FullName)" -ForegroundColor Cyan
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " Build Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

exit 0
