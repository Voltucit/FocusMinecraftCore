using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using FMCore.Downloader;

namespace FMCore.Java;

/// <summary>使用厂商官方公开服务的 Java 发行版；不包含需要订阅凭据的下载。</summary>
public enum JavaDistribution { Temurin, Zulu }
/// <summary>下载的运行环境类型；JRE 用于运行游戏，JDK 同时包含开发工具。</summary>
public enum JavaPackageType { Jre, Jdk }

/// <summary>官方可下载版本的不可变描述；查询不下载文件，可直接传入 InstallAsync 精确安装。</summary>
[DebuggerDisplay("{Distribution} {FullVersion} {Architecture} {PackageType}")]
public sealed class JavaRuntimeVersion
{
    /// <summary>厂商包标识，用于区分同一 Java 版本的不同发行构建。</summary>
    public string Id { get; }
    /// <summary>含补丁和构建号的完整 Java 版本，例如 21.0.12.1+1-LTS。</summary>
    public string FullVersion { get; }
    /// <summary>Java 主版本。</summary>
    public int MajorVersion => Version.Major;
    /// <summary>发行版。</summary>
    public JavaDistribution Distribution { get; }
    /// <summary>Windows 目标架构。</summary>
    public JavaArchitecture Architecture { get; }
    /// <summary>JRE 或 JDK。</summary>
    public JavaPackageType PackageType { get; }
    /// <summary>厂商公布的 ZIP 大小（字节）；SizeIsApproximate 为 true 时仅用于展示。</summary>
    public long Size { get; }
    /// <summary>Zulu 元数据大小可能近似到百字节；此时下载仍严格校验实际响应长度及厂商 SHA-256。</summary>
    public bool SizeIsApproximate => Distribution == JavaDistribution.Zulu;
    /// <summary>厂商元数据公布的 SHA-256。</summary>
    public string Sha256 { get; }
    /// <summary>厂商公布的 HTTPS 下载地址。</summary>
    public Uri DownloadUri { get; }
    internal JavaVersion Version { get; }
    internal JavaRuntimeVersion(string id, string version, JavaDistribution distribution, JavaArchitecture architecture,
        JavaPackageType packageType, long size, string sha256, Uri downloadUri)
    {
        Id = id; FullVersion = version; Version = JavaVersion.Parse(version); Distribution = distribution;
        Architecture = architecture; PackageType = packageType; Size = size; Sha256 = sha256.ToLowerInvariant(); DownloadUri = downloadUri;
    }
    /// <summary>仅显示发行版、版本及架构，不包含 URL。</summary>
    public override string ToString() => $"{Distribution} {FullVersion} {Architecture} {PackageType}";
}

/// <summary>版本筛选及安装配置；默认 Temurin JRE、当前 Windows 架构和用户级托管目录。</summary>
[DebuggerDisplay("Java Runtime options")]
public sealed record JavaRuntimeOptions
{
    /// <summary>查询或按主版本安装时使用的发行版，默认 Temurin。</summary>
    public JavaDistribution Distribution { get; init; } = JavaDistribution.Temurin;
    /// <summary>目标架构，默认操作系统架构；仅支持 x86、x64、ARM64。</summary>
    public JavaArchitecture Architecture { get; init; } = RuntimeInformation.OSArchitecture switch
    { System.Runtime.InteropServices.Architecture.X86 => JavaArchitecture.X86, System.Runtime.InteropServices.Architecture.Arm64 => JavaArchitecture.Arm64, _ => JavaArchitecture.X64 };
    /// <summary>JRE 或 JDK，默认 JRE。</summary>
    public JavaPackageType PackageType { get; init; } = JavaPackageType.Jre;
    /// <summary>可选完整版本，按版本及构建号精确筛选；建议使用查询结果的 FullVersion。</summary>
    public string? FullVersion { get; init; }
    /// <summary>托管安装根目录，内部按包隔离；null 使用 DefaultInstallationDirectory，不指向系统 Java。</summary>
    public string? InstallationDirectory { get; init; }
    /// <summary>单次官方元数据请求超时，默认 30 秒，最大 5 分钟。</summary>
    public TimeSpan QueryTimeout { get; init; } = TimeSpan.FromSeconds(30);
    /// <summary>实际运行 Java 探测的超时，默认 10 秒，最大 1 分钟。</summary>
    public TimeSpan ProbeTimeout { get; init; } = TimeSpan.FromSeconds(10);
    /// <summary>复用 Downloader 的重试、超时及下载配置。</summary>
    public DownloadOptions Downloads { get; init; } = new();
    /// <summary>可选调用方拥有的 HTTP 客户端，用于代理或模拟；核心不会释放它。请勿设置账号凭据默认头。</summary>
    [JsonIgnore] public HttpClient? HttpClient { get; init; }
    /// <summary>不输出路径或传输配置。</summary>
    public override string ToString() => "Java Runtime options";
}

/// <summary>Java Runtime 显式安装阶段。</summary>
public enum JavaRuntimeStage { Querying, Recovering, Verifying, Downloading, Extracting, Validating, Committing, Completed, Reused }
/// <summary>安装阶段、文件计数及可选下载字节进度；回调可能来自后台线程。</summary>
/// <param name="Stage">当前阶段。</param>
/// <param name="CompletedFiles">已处理文件数。</param>
/// <param name="TotalFiles">本阶段文件总数。</param>
/// <param name="Download">复用 Downloader 的字节进度。</param>
public sealed record JavaRuntimeProgress(JavaRuntimeStage Stage, int CompletedFiles = 0, int TotalFiles = 0, DownloadProgress? Download = null);
