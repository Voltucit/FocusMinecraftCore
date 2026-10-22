using System.Net;
using System.Text.Json;

namespace FMCore.Java;

internal static class RuntimeCatalog
{
    internal static readonly HttpClient Http = new(new SocketsHttpHandler { AllowAutoRedirect = true }) { Timeout = Timeout.InfiniteTimeSpan };
    private const int PageSize = 100;
    internal static JavaRuntimeOptions Options(JavaRuntimeOptions? options)
    {
        if (!OperatingSystem.IsWindows()) throw new JavaException(JavaErrorCode.UnsupportedPlatform, "Java Runtime 安装与查询目前仅支持 Windows。");
        options ??= new();
        if (!Enum.IsDefined(options.Distribution) || !Enum.IsDefined(options.PackageType) || options.Architecture is not (JavaArchitecture.X86 or JavaArchitecture.X64 or JavaArchitecture.Arm64) ||
            options.QueryTimeout <= TimeSpan.Zero || options.QueryTimeout > TimeSpan.FromMinutes(5) || options.ProbeTimeout <= TimeSpan.Zero ||
            options.ProbeTimeout > TimeSpan.FromMinutes(1) || options.Downloads == null) throw InvalidOptions();
        if (options.FullVersion != null)
        {
            try { if (JavaVersion.Parse(options.FullVersion).IsEarlyAccess) throw InvalidOptions(); }
            catch (JavaException) { throw InvalidOptions(); }
        }
        return options with { Downloads = options.Downloads with { } };
    }

    internal static async Task<IReadOnlyList<JavaRuntimeVersion>> GetAsync(int? major, JavaRuntimeOptions options, CancellationToken ct)
    {
        try { return await GetCoreAsync(major, options, ct).ConfigureAwait(false); }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException || e is JavaException { Code: JavaErrorCode.InvalidOutput }) { throw Metadata(); }
    }
    private static async Task<IReadOnlyList<JavaRuntimeVersion>> GetCoreAsync(int? major, JavaRuntimeOptions options, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (major is < 1 or > 999) throw InvalidOptions();
        var results = new List<JavaRuntimeVersion>();
        if (options.Distribution == JavaDistribution.Temurin)
        {
            int[] majors;
            if (major.HasValue) majors = [major.Value];
            else
            {
                using var available = await JsonAsync("https://api.adoptium.net/v3/info/available_releases", options, ct).ConfigureAwait(false) ?? throw Metadata();
                majors = Array(available.RootElement.GetProperty("available_releases")).Select(v => v.GetInt32()).Distinct().ToArray();
                if (majors.Length > 100 || majors.Any(m => m is < 1 or > 999)) throw Metadata();
            }
            foreach (var feature in majors)
                for (var page = 0; ; page++)
                {
                    if (page >= 100) throw Metadata();
                    using var json = await JsonAsync($"https://api.adoptium.net/v3/assets/feature_releases/{feature}/ga?architecture={AdoptiumArch(options.Architecture)}&heap_size=normal&image_type={Kind(options.PackageType)}&jvm_impl=hotspot&os=windows&page={page}&page_size=20&project=jdk&vendor=eclipse", options, ct).ConfigureAwait(false);
                    if (json == null) break;
                    var releases = Array(json.RootElement);
                    foreach (var release in releases) ParseTemurin(release, options, results);
                    if (releases.Length < 20) break;
                }
        }
        else
        {
            for (var page = 1; ; page++)
            {
                if (page > 100) throw Metadata();
                var filter = major.HasValue ? $"java_version={major}&" : "";
                using var json = await JsonAsync($"https://api.azul.com/metadata/v1/zulu/packages/?{filter}os=windows&arch={AzulArch(options.Architecture)}&java_package_type={Kind(options.PackageType)}&javafx_bundled=false&crac_supported=false&crs_supported=false&archive_type=zip&release_status=ga&availability_types=CA&page={page}&page_size={PageSize}&include_fields=sha256_hash,size,os,arch,hw_bitness,java_package_type,release_status,archive_type", options, ct).ConfigureAwait(false);
                if (json == null) break;
                var packages = Array(json.RootElement);
                foreach (var package in packages) ParseZulu(package, options, results);
                if (packages.Length < PageSize) break;
            }
        }
        var requested = options.FullVersion == null ? null : JavaVersion.Parse(options.FullVersion);
        return System.Array.AsReadOnly(results.Where(v => (!major.HasValue || v.MajorVersion == major) && (requested == null || v.Version.CompareTo(requested) == 0))
            .DistinctBy(v => v.Distribution + ":" + v.Id).OrderByDescending(v => v.Version).ThenBy(v => v.Id, StringComparer.Ordinal).ToArray());
    }

    private static void ParseTemurin(JsonElement release, JavaRuntimeOptions options, List<JavaRuntimeVersion> results)
    {
        try
        {
            if (Text(release, "release_type") != "ga" || Text(release, "vendor") != "eclipse") return;
            var full = Text(release.GetProperty("version_data"), "openjdk_version");
            foreach (var binary in Array(release.GetProperty("binaries")))
            {
                if (Text(binary, "os") != "windows" || Text(binary, "architecture") != AdoptiumArch(options.Architecture) || Text(binary, "image_type") != Kind(options.PackageType) ||
                    Text(binary, "jvm_impl") != "hotspot" || Text(binary, "heap_size") != "normal" || Text(binary, "project") != "jdk") continue;
                var package = binary.GetProperty("package");
                if (!Text(package, "name").EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                var value = Create(Text(release, "release_name") + ":" + Text(package, "name"), full, options, package.GetProperty("size").GetInt64(), Text(package, "checksum"), Text(package, "link"));
                results.Add(value);
            }
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException) { throw Metadata(); }
    }
    private static void ParseZulu(JsonElement package, JavaRuntimeOptions options, List<JavaRuntimeVersion> results)
    {
        try
        {
            var bits = package.GetProperty("hw_bitness").ToString(); var arch = Text(package, "arch");
            var expectedArch = options.Architecture == JavaArchitecture.Arm64 ? "arm" : "x86";
            if (Text(package, "availability_type") != "CA" || Text(package, "product") != "zulu" || Text(package, "release_status") != "ga" || Text(package, "os") != "windows" ||
                Text(package, "archive_type") != "zip" || Text(package, "java_package_type") != Kind(options.PackageType) || arch != expectedArch || bits != (options.Architecture == JavaArchitecture.X86 ? "32" : "64")) return;
            var parts = Array(package.GetProperty("java_version")).Select(v => v.GetInt32()).ToArray();
            if (parts.Length is < 1 or > 4 || parts.Any(v => v < 0)) throw Metadata();
            var build = package.GetProperty("openjdk_build_number").GetInt32(); if (build < 0) throw Metadata();
            var full = string.Join('.', parts) + "+" + build;
            var id = Text(package, "package_uuid"); if (!Guid.TryParse(id, out _)) throw Metadata();
            results.Add(Create(id, full, options, package.GetProperty("size").GetInt64(), Text(package, "sha256_hash"), Text(package, "download_url")));
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException) { throw Metadata(); }
    }
    private static JavaRuntimeVersion Create(string id, string full, JavaRuntimeOptions options, long size, string hash, string url)
    {
        if (id.Length is < 1 or > 1024 || id.Any(char.IsControl) || size is < 1 or > 2L * 1024 * 1024 * 1024 || hash.Length != 64 || !hash.All(char.IsAsciiHexDigit) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) throw Metadata();
        var version = new JavaRuntimeVersion(id, full, options.Distribution, options.Architecture, options.PackageType, size, hash, uri);
        Validate(version);
        return version;
    }
    internal static void Validate(JavaRuntimeVersion version)
    {
        if (version == null || version.Version.IsEarlyAccess || version.Size is < 1 or > 2L * 1024 * 1024 * 1024 || version.Sha256.Length != 64 || !version.Sha256.All(char.IsAsciiHexDigit) ||
            !Enum.IsDefined(version.Distribution) || !Enum.IsDefined(version.PackageType) || version.Architecture is not (JavaArchitecture.X86 or JavaArchitecture.X64 or JavaArchitecture.Arm64)) throw Metadata();
        var uri = version.DownloadUri;
        if (uri.Scheme != "https" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || !uri.AbsolutePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            !(version.Distribution == JavaDistribution.Temurin ? uri.Host == "github.com" && uri.AbsolutePath.StartsWith($"/adoptium/temurin{version.MajorVersion}-binaries/releases/download/", StringComparison.Ordinal) : uri.Host == "cdn.azul.com" && uri.AbsolutePath.StartsWith("/zulu/bin/", StringComparison.Ordinal))) throw Metadata();
    }
    private static async Task<JsonDocument?> JsonAsync(string url, JavaRuntimeOptions options, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(options.QueryTimeout);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url); request.Headers.UserAgent.ParseAdd("FMCore/0.1");
            using var response = await (options.HttpClient ?? Http).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (response.RequestMessage?.RequestUri is { } final && (final.Scheme != "https" || final.Host != new Uri(url).Host)) throw Metadata();
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode) throw new JavaException(JavaErrorCode.RuntimeQueryFailed, "Java 官方版本服务返回失败状态。");
            if (response.Content.Headers.ContentLength > 8 * 1024 * 1024) throw Metadata();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream(); var bytes = new byte[81920]; int count;
            while ((count = await stream.ReadAsync(bytes, timeout.Token).ConfigureAwait(false)) != 0)
            { if (buffer.Length + count > 8 * 1024 * 1024) throw Metadata(); await buffer.WriteAsync(bytes.AsMemory(0, count), timeout.Token).ConfigureAwait(false); }
            return JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), new() { MaxDepth = 32 });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new JavaException(JavaErrorCode.RuntimeQueryTimeout, "Java 官方版本查询超时。"); }
        catch (Exception e) when (e is HttpRequestException or IOException) { throw new JavaException(JavaErrorCode.RuntimeQueryFailed, "无法读取 Java 官方版本服务。"); }
        catch (JsonException) { throw Metadata(); }
    }
    private static JsonElement[] Array(JsonElement value) => value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : throw Metadata();
    private static string Text(JsonElement value, string key) => value.GetProperty(key).GetString() ?? throw Metadata();
    private static string Kind(JavaPackageType kind) => kind == JavaPackageType.Jre ? "jre" : "jdk";
    private static string AdoptiumArch(JavaArchitecture arch) => arch switch { JavaArchitecture.X86 => "x32", JavaArchitecture.Arm64 => "aarch64", _ => "x64" };
    private static string AzulArch(JavaArchitecture arch) => arch switch { JavaArchitecture.X86 => "i686", JavaArchitecture.Arm64 => "aarch64", _ => "x64" };
    internal static JavaException Metadata() => new(JavaErrorCode.RuntimeMetadataInvalid, "Java 官方元数据缺少有效版本、架构、大小、校验或受信任下载地址。");
    private static JavaException InvalidOptions() => new(JavaErrorCode.RuntimeInvalidOptions, "Java Runtime 筛选、超时或架构配置无效。");
}
