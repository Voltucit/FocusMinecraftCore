param(
    [string]$PackageDirectory = (Join-Path $PSScriptRoot '../artifacts/packages'),
    [string]$WorkDirectory = (Join-Path $PSScriptRoot '../artifacts/package-validation')
)
$ErrorActionPreference = 'Stop'
$PackageDirectory = [IO.Path]::GetFullPath($PackageDirectory)
$WorkDirectory = [IO.Path]::GetFullPath($WorkDirectory)
$project = [xml](Get-Content -LiteralPath (Join-Path $PSScriptRoot '../FMCore/FMCore.csproj') -Raw)
$version = [string]$project.Project.PropertyGroup.Version
$packageId = [string]$project.Project.PropertyGroup.PackageId
$package = Join-Path $PackageDirectory "$packageId.$version.nupkg"
if (!(Test-Path -LiteralPath $package)) { throw "Package not found: $package" }
# 新建独立目录，隔离包缓存，不接受项目引用或已有 FMCore 缓存冒充包验证。
$consumer = Join-Path $WorkDirectory ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $consumer -Force | Out-Null
$escapedVersion = [System.Security.SecurityElement]::Escape($version)
$escapedPackageId = [System.Security.SecurityElement]::Escape($packageId)
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="$escapedPackageId" Version="[$escapedVersion]" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'PackageConsumer.csproj') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PackageSmoke.cs') -Destination (Join-Path $consumer 'Program.cs')
$consumerProject = Join-Path $consumer 'PackageConsumer.csproj'
$escapedSource = [System.Security.SecurityElement]::Escape($PackageDirectory)
@"
<configuration><packageSources><clear />
  <add key="fmcore-local" value="$escapedSource" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources></configuration>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'NuGet.Config') -Encoding utf8
dotnet restore $consumerProject --configfile (Join-Path $consumer 'NuGet.Config') --packages (Join-Path $consumer 'packages')
if ($LASTEXITCODE -ne 0) { throw 'Independent package restore failed.' }
dotnet run --project $consumerProject -c Release --no-restore -- (Join-Path $consumer 'data')
if ($LASTEXITCODE -ne 0) { throw 'Public package API validation failed.' }
Write-Output "Independent package consumer passed: $consumer"
