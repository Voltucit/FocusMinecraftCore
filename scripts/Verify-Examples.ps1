param(
    [string]$PackageDirectory = (Join-Path $PSScriptRoot '../artifacts/packages'),
    [string]$WorkDirectory = (Join-Path $PSScriptRoot '../artifacts/example-validation')
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$PackageDirectory = [IO.Path]::GetFullPath($PackageDirectory)
$WorkDirectory = Join-Path ([IO.Path]::GetFullPath($WorkDirectory)) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null
$project = [xml](Get-Content -LiteralPath (Join-Path $root 'FMCore/FMCore.csproj') -Raw)
$version = $project.Project.PropertyGroup.Version
$packageId = [Security.SecurityElement]::Escape([string]$project.Project.PropertyGroup.PackageId)
$escapedSource = [Security.SecurityElement]::Escape($PackageDirectory)
@"
<configuration><packageSources><clear />
  <add key="fmcore-local" value="$escapedSource" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources></configuration>
"@ | Set-Content -LiteralPath (Join-Path $WorkDirectory 'NuGet.Config') -Encoding utf8
$documents = @('README.md', 'docs/sdk.md', 'docs/downloader.md', 'docs/versions.md',
    'docs/java.md', 'docs/installer.md', 'docs/authentication.md', 'docs/launch.md')
$count = 0
foreach ($document in $documents) {
    $text = Get-Content -LiteralPath (Join-Path $root $document) -Raw
    $blocks = [regex]::Matches($text, '(?s)```csharp\s*\r?\n(.*?)```')
    $blockIndex = 0
    foreach ($block in $blocks) {
        $blockIndex++
        $count++
        $code = $block.Groups[1].Value
        # 只补充文档明确由调用方提供的变量；不修改示例 API、参数或调用顺序。
        $prefix = ''
        if ($document -eq 'docs/sdk.md') {
            $prefix = 'var ct = CancellationToken.None; var accountDirectory = Path.Combine(Path.GetTempPath(), "FMCoreExamples");'
        }
        if ($document -eq 'docs/authentication.md' -and $blockIndex -eq 1) {
            $prefix = 'using FMCore.Authentication; var accountDirectory = Path.Combine(Path.GetTempPath(), "FMCoreExamples");'
        }
        if ($document -eq 'docs/versions.md' -and $blockIndex -eq 2) {
            $prefix = 'using FMCore.Downloader; using FMCore.Versions; var versions = new VersionManager(new DownloadManager(".minecraft")); using var cancellation = new CancellationTokenSource();'
        }
        # using 必须位于顶层语句之前。
        $usingLines = [regex]::Matches($code, '(?m)^using [A-Za-z_][A-Za-z0-9_.]*;\s*\r?\n')
        $usings = ($usingLines | ForEach-Object { $_.Value }) -join ''
        $body = [regex]::Replace($code, '(?m)^using [A-Za-z_][A-Za-z0-9_.]*;\s*\r?\n', '')
        $folder = Join-Path $WorkDirectory "Example$count"
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        "$usings`n$prefix`n$body" | Set-Content -LiteralPath (Join-Path $folder 'Program.cs') -Encoding utf8
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally><IsPackable>false</IsPackable>
    <!-- 只声明用于接入的局部示例函数，无需为了编译而执行下载/认证。 -->
    <NoWarn>CS8321</NoWarn>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="$packageId" Version="[$version]" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $folder 'Example.csproj') -Encoding utf8
        $exampleProject = Join-Path $folder 'Example.csproj'
        dotnet restore $exampleProject --configfile (Join-Path $WorkDirectory 'NuGet.Config') --packages (Join-Path $WorkDirectory 'packages') --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Example restore failed: $document block $blockIndex" }
        dotnet build $exampleProject -c Release --no-restore --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Example compile failed: $document block $blockIndex" }
        Write-Output "Compiled: $document block $blockIndex"
    }
}
Write-Output "Compiled $count C# examples against $packageId $version using only PackageReference. Examples were not executed."
