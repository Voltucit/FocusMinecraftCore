namespace FMCore.Java;

/// <summary>Windows Java Runtime 官方版本查询与显式安装入口；不会为游戏自动推荐主版本或修改系统 Java。</summary>
public static class JavaRuntimeUtil
{
    /// <summary>默认用户级托管根目录；每个精确发行包拥有独立子目录。</summary>
    public static string DefaultInstallationDirectory => RuntimeFiles.DefaultDirectory;

    /// <summary>查询官方 GA 版本，不创建目录、不下载 ZIP 或安装；主版本为 null 时查询该发行版的可用版本。</summary>
    /// <param name="majorVersion">可选主版本筛选，例如 21。</param>
    /// <param name="options">发行版、架构、JRE/JDK、完整版本及传输配置。</param>
    /// <param name="cancellationToken">取消查询。</param>
    /// <returns>按版本从新到旧排列的不可变描述；无匹配版本时为空。</returns>
    /// <exception cref="JavaException">请求、超时、元数据、配置或平台失败，通过 Code 区分。</exception>
    public static Task<IReadOnlyList<JavaRuntimeVersion>> GetAvailableVersionsAsync(int? majorVersion = null,
        JavaRuntimeOptions? options = null, CancellationToken cancellationToken = default) =>
        RuntimeCatalog.GetAsync(majorVersion, RuntimeCatalog.Options(options), cancellationToken);

    /// <summary>显式安装指定主版本中符合筛选条件的最新 GA 包；不推断 Minecraft 的 Java 要求。</summary>
    /// <param name="majorVersion">调用方选择的 Java 主版本。</param>
    /// <param name="options">可指定发行版、完整版本、架构、JRE/JDK、安装根目录及传输策略。</param>
    /// <param name="progress">可选阶段与字节进度。</param>
    /// <param name="cancellationToken">取消查询、下载、解压及探测；提交前取消保留原安装。</param>
    /// <returns>实际运行验证后的现有 JavaRuntime，可将 ExecutablePath 传给 JavaConfig.JavaPath。</returns>
    public static async Task<JavaRuntime> InstallAsync(int majorVersion, JavaRuntimeOptions? options = null,
        IProgress<JavaRuntimeProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var settings = RuntimeCatalog.Options(options);
        progress?.Report(new(JavaRuntimeStage.Querying));
        var versions = await RuntimeCatalog.GetAsync(majorVersion, settings, cancellationToken).ConfigureAwait(false);
        var selected = versions.FirstOrDefault() ?? throw new JavaException(JavaErrorCode.RuntimeNotAvailable, "官方来源没有符合条件的 Windows Java 包。");
        return await InstallAsync(selected, settings, progress, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>精确安装查询结果，不重新选择版本；完整且探测通过的同包直接复用，损坏文件按原包修复。</summary>
    /// <param name="version">GetAvailableVersionsAsync 返回的精确包，决定发行版、版本、架构及类型。</param>
    /// <param name="options">安装目录、超时、下载及传输配置；发行版、架构和版本筛选不覆盖精确包。</param>
    /// <param name="progress">可选阶段与字节进度。</param>
    /// <param name="cancellationToken">取消安装，支持失败恢复。</param>
    /// <returns>复用 T03 实际探测得到的 JavaRuntime；不创建第二套 JavaInfo 模型。</returns>
    /// <exception cref="JavaException">通过 Code 报告下载、校验、归档、运行、路径、并发和恢复失败。</exception>
    public static Task<JavaRuntime> InstallAsync(JavaRuntimeVersion version, JavaRuntimeOptions? options = null,
        IProgress<JavaRuntimeProgress>? progress = null, CancellationToken cancellationToken = default) =>
        RuntimeInstallation.InstallAsync(version, RuntimeCatalog.Options(options), progress, cancellationToken);
}
