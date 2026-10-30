using System.ComponentModel;
using System.Text.Json;
using FMCore.Accounts;
using FMCore.Java;
using FMCore.Logger;
using FMCore.Versions;

namespace FMCore.Launch;

public sealed partial class MinecraftLauncher
{
    public async Task<LaunchCheckResult> CheckAsync(
        Action<LaunchProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException(
                "这个启动器实例正在检查、启动或运行游戏，请等待结束。");

        try
        {
            return await Task.Run(
                () => CheckCoreAsync(onProgress, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<LaunchCheckResult> CheckCoreAsync(
        Action<LaunchProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        JavaInfo? java = null;
        bool? modern = null;

        void Report(LaunchStage stage, string message)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(new LaunchProgress(stage, message));
            cancellationToken.ThrowIfCancellationRequested();
        }

        LaunchCheckResult Failure(LaunchStage stage, Exception error)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string message = error.Message;
            string? token = _config.Account.AccessToken;

            // 诊断不返回构造出的命令行；对消息中的已知账号令牌做脱敏。
            if (!string.IsNullOrEmpty(token) && token != "0")
                message = message.Replace(token, "[已隐藏令牌]", StringComparison.Ordinal);

            return new LaunchCheckResult(
                _config.GameCoreConfig.Version, java, modern,
                new LaunchCheckIssue(stage, message, error.GetType().Name));
        }

        Report(LaunchStage.ReadingVersion, "正在检查版本与继承链。");

        MinecraftVersion version;
        try
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("当前启动检查只支持 Windows。");

            version = await new MinecraftVersionReader().GetVersionAsync(
                _config.GameCoreConfig.Version, _root,
                cancellationToken).ConfigureAwait(false);

            modern = LibraryResolver.GetValidatedChain(version, _root)
                .Any(item => MinecraftVersionReader.ReadVersionJson(
                    item.Directory).ContainsKey("arguments"));
        }
        catch (Exception error) when (IsExpectedCheckError(error))
        {
            return Failure(LaunchStage.ReadingVersion, error);
        }

        Report(LaunchStage.DetectingJava, "正在探测所选 Java。");
        try
        {
            java = await JavaReader.GetJavaInfoAsync(
                _javaPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (IsExpectedCheckError(error))
        {
            return Failure(LaunchStage.DetectingJava, error);
        }

        Report(LaunchStage.PreparingGame, "正在检查启动参数与所需文件路径。");
        try
        {
            PreparedLaunchConfig prepared = new(
                version, java, _config.Account,
                _config.JavaConfig.MaxMemoryMb,
                gameDirectory: _gameDirectory,
                windowWidth: _config.GameWindowConfig?.Width,
                windowHeight: _config.GameWindowConfig?.Height,
                jvmArguments: _jvmArguments,
                authlibInjectorPath: _authlibInjectorPath)
            {
                Fullscreen = _config.GameWindowConfig?.Fullscreen ?? false,
                GameArguments = _gameArguments
            };

            if (_config.Account is MicrosoftAccount account
                && account.ExpiresAt <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Microsoft 登录已过期，请恢复授权或重新登录。");

            if (modern == true)
            {
                // 只向构造器提供将来可用的路径，不创建此目录。
                string natives = Path.Combine(
                    version.Directory, $"natives-{Guid.NewGuid():N}");
                _ = ModernArgumentBuilder.Build(prepared, _root, natives);
            }
            else
            {
                // 与旧版启动在解压原生库前执行的检查保持一致。
                _ = LegacyArgumentBuilder.GetGameArguments(prepared, _root);
                _ = LibraryResolver.GetWindowsClassPath(version, _root);
                _ = LaunchResourceResolver.ResolveLogging(version, _root);
                _ = LibraryResolver.GetWindowsNativeLibraryPaths(version, _root, java);
            }
        }
        catch (Exception error) when (IsExpectedCheckError(error))
        {
            return Failure(LaunchStage.PreparingGame, error);
        }

        // 回调在诊断异常的捕获范围之外，回调失败继续传播。
        Report(LaunchStage.PreparingGame, "启动配置检查完成。");
        return new LaunchCheckResult(
            _config.GameCoreConfig.Version, java, modern, Issue: null);
    }

    private static bool IsExpectedCheckError(Exception error) => error is
        IOException or UnauthorizedAccessException or ArgumentException
        or InvalidOperationException or NotSupportedException
        or KeyNotFoundException or JsonException
        or Win32Exception or TimeoutException;
}
