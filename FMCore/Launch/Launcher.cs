using System.Diagnostics;
using System.Text;
using FMCore.Versions;
using FMCore.Accounts;
namespace FMCore.Launch;

public static class Launcher
{
    /// <summary>
    /// 启动旧版原版游戏，持续读取日志，并等待游戏退出。
    /// 返回 Java 进程退出码；取消时终止本次进程并抛出取消异常。
    /// </summary>
    public static Task<int> RunAsync(
    PreparedLaunchConfig config,
    string minecraftRoot,
    Action<string>? onOutput = null,
    Action<string>? onError = null,
    CancellationToken cancellationToken = default)
    => RunCoreAsync(
        config,
        minecraftRoot,
        false,
        onOutput,
        onError,
        cancellationToken);


// 保留原来的旧版入口。
public static Task<int> RunLegacyAsync(
    PreparedLaunchConfig config,
    string minecraftRoot,
    Action<string>? onOutput = null,
    Action<string>? onError = null,
    CancellationToken cancellationToken = default)
    => RunCoreAsync(
        config,
        minecraftRoot,
        true,
        onOutput,
        onError,
        cancellationToken);

private static async Task<int> RunCoreAsync(
    PreparedLaunchConfig config,
    string minecraftRoot,
    bool legacyOnly,
    Action<string>? onOutput,
    Action<string>? onError,
    CancellationToken cancellationToken,
    Action<LaunchProgress>? onProgress = null)
{
    ArgumentNullException.ThrowIfNull(config);
    ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);
    ArgumentException.ThrowIfNullOrWhiteSpace(
        config.Java.Executable);

    cancellationToken.ThrowIfCancellationRequested();

    if (!OperatingSystem.IsWindows())
        throw new PlatformNotSupportedException(
            "当前启动流程只支持 Windows。");

    if (!Path.IsPathFullyQualified(config.Java.Executable))
        throw new ArgumentException(
            "所选 Java 必须使用完整路径。");

    if (!File.Exists(config.Java.Executable))
        throw new FileNotFoundException(
            "找不到所选 Java。", config.Java.Executable);

    onProgress?.Invoke(new LaunchProgress(
        LaunchStage.PreparingGame,
        "正在检查启动文件并准备原生库。"));

    string[] accountJvmArguments = [];

    if (config.Account is YggdrasilAccount externalAccount)
    {
        onProgress?.Invoke(new LaunchProgress(
            LaunchStage.PreparingGame,
            "正在检查外置验证凭证并读取验证服务配置。"));

        accountJvmArguments =
            await YggdrasilAuthenticator.GetLaunchArgumentsAsync(
                externalAccount,
                config.AuthlibInjectorPath!,
                cancellationToken).ConfigureAwait(false);
    }

    bool modern = !legacyOnly
        && LibraryResolver.GetValidatedChain(
                config.Version, minecraftRoot)
            .Any(item =>
                MinecraftVersionReader.ReadVersionJson(
                    item.Directory).ContainsKey("arguments"));

    ProcessStartInfo startInfo;

    if (modern)
    {
        string nativesDirectory = Path.Combine(
            Path.GetFullPath(config.Version.Directory),
            $"natives-{Guid.NewGuid():N}");

        if (Path.Exists(nativesDirectory))
            throw new IOException(
                "本次原生库工作目录已经存在。");

        // 先完成文件检查和参数构造，再创建输出目录。
        startInfo = ModernArgumentBuilder.Build(
            config,
            minecraftRoot,
            nativesDirectory);

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(nativesDirectory);
    }
    else
    {
        _ = LegacyArgumentBuilder.GetGameArguments(
            config, minecraftRoot);

        _ = LibraryResolver.GetWindowsClassPath(
            config.Version, minecraftRoot);

        _ = LaunchResourceResolver.ResolveLogging(
            config.Version, minecraftRoot);

        cancellationToken.ThrowIfCancellationRequested();

        string? nativesDirectory =
            await NativeExtractor.ExtractWindowsAsync(
                config.Version,
                minecraftRoot,
                config.Java,
                cancellationToken).ConfigureAwait(false);

        startInfo = LegacyArgumentBuilder.Build(
            config,
            minecraftRoot,
            nativesDirectory);
    }

    // 保持调用方提供的顺序，每个字符串作为独立参数传入。
    for (int index = 0;
         index < config.JvmArguments.Count;
         index++)
    {
        startInfo.ArgumentList.Insert(
            index,
            config.JvmArguments[index]);
    }

    startInfo.ArgumentList.Insert(
        config.JvmArguments.Count,
        "-Dfile.encoding=UTF-8");

    int accountArgumentIndex = config.JvmArguments.Count + 1;

    foreach (string argument in accountJvmArguments)
    {
        startInfo.ArgumentList.Insert(accountArgumentIndex++, argument);
    }

    startInfo.StandardOutputEncoding = Encoding.UTF8;
    startInfo.StandardErrorEncoding = Encoding.UTF8;

    foreach (string key in new[]
             {
                 "JAVA_TOOL_OPTIONS",
                 "JDK_JAVA_OPTIONS",
                 "_JAVA_OPTIONS"
             })
    {
        startInfo.Environment.Remove(key);
    }

    cancellationToken.ThrowIfCancellationRequested();

    if (config.Account is MicrosoftAccount account
        && account.ExpiresAt <= DateTimeOffset.UtcNow)
    {
        throw new InvalidOperationException(
            "Microsoft 登录已过期，请重新登录后启动。");
    }
    Directory.CreateDirectory(startInfo.WorkingDirectory);

    int exitCode = await RunProcessAsync(
        startInfo,
        onOutput,
        onError,
        cancellationToken,
        onProgress).ConfigureAwait(false);

    onProgress?.Invoke(new LaunchProgress(
        LaunchStage.GameExited,
        $"Java 进程已退出，退出码：{exitCode}。"));

    return exitCode;
}

    private static async Task<int> RunProcessAsync(
    ProcessStartInfo startInfo,
    Action<string>? onOutput,
    Action<string>? onError,
    CancellationToken cancellationToken,
    Action<LaunchProgress>? onProgress)
    {
        using Process process = new()
        {
            StartInfo = startInfo
        };

        using CancellationTokenSource stopReading = new();

        Task outputTask = Task.CompletedTask;
        Task errorTask = Task.CompletedTask;
        bool started = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            onProgress?.Invoke(new LaunchProgress(
                LaunchStage.StartingJava,
                "正在创建 Java 进程。"));


            cancellationToken.ThrowIfCancellationRequested();

            if (!process.Start())
                throw new InvalidOperationException(
                    "未能创建 Java 进程。");

            started = true;

            onProgress?.Invoke(new LaunchProgress(
                LaunchStage.JavaStarted,
                $"Java 进程已创建，PID：{process.Id}。"));


            // 两个流同时读取，避免其中一个缓冲区写满。
            outputTask = ReadLinesAsync(
                process.StandardOutput,
                onOutput,
                stopReading.Token);

            errorTask = ReadLinesAsync(
                process.StandardError,
                onError,
                stopReading.Token);

            Task exitTask =
                process.WaitForExitAsync(CancellationToken.None);

            List<Task> pending =
                [outputTask, errorTask, exitTask];

            while (pending.Count > 0)
            {
                Task finished = await Task.WhenAny(pending)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 日志读取或回调失败，也及时进入收尾流程。
                await finished.ConfigureAwait(false);
                pending.Remove(finished);

                if (finished == exitTask)
                {
                    // 进程退出后，给剩余日志一个有限的排空时间。
                    await Task.WhenAll(pending)
                        .WaitAsync(
                            TimeSpan.FromSeconds(5),
                            cancellationToken)
                        .ConfigureAwait(false);

                    break;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return process.ExitCode;
        }
        catch (Exception originalError)
        {
            List<Exception> cleanupErrors = [];

            if (started)
            {
                try
                {
                    await StopProcessAsync(process)
                        .ConfigureAwait(false);
                }
                catch (Exception error)
                {
                    cleanupErrors.Add(new IOException(
                        "停止游戏进程失败，游戏可能仍在运行。",
                        error));
                }
            }

            stopReading.Cancel();

            try
            {
                await Task.WhenAll(outputTask, errorTask)
                    .WaitAsync(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (stopReading.IsCancellationRequested)
            {
                // 收尾时主动取消日志读取，属于预期情况。
            }
            catch (Exception error)
            {
                if (!ReferenceEquals(error, originalError))
                    cleanupErrors.Add(error);
            }

            if (cleanupErrors.Count > 0)
            {
                cleanupErrors.Insert(0, originalError);

                throw new AggregateException(
                    "游戏运行失败，且进程或日志收尾出现错误。",
                    cleanupErrors);
            }

            throw;
        }
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        Action<string>? receive,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            string? line = await reader
                .ReadLineAsync(cancellationToken)
                .ConfigureAwait(false);

            if (line is null)
                return;

            receive?.Invoke(line);
        }
    }

    private static async Task StopProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) when (process.HasExited)
        {
            // 检查与停止之间，进程可能刚好自己退出。
        }

        await process.WaitForExitAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10))
            .ConfigureAwait(false);
    }

    internal static Task<int> RunWithProgressAsync(
        PreparedLaunchConfig config,
        string minecraftRoot,
        Action<LaunchProgress>? onProgress,
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken)
        => RunCoreAsync(
            config,
            minecraftRoot,
            false,
            onOutput,
            onError,
            cancellationToken,
            onProgress);
}
