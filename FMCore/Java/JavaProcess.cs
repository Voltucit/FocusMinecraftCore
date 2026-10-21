using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FMCore.Java;

internal sealed record JavaProcessOutput(int ExitCode, string StandardOutput, string StandardError);

internal static class JavaProcess
{
    internal static async Task<JavaProcessOutput> RunAsync(ProcessStartInfo start, TimeSpan timeout, int outputLimit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lifetime.CancelAfter(timeout);
        using var process = new Process { StartInfo = start };
        var started = false;
        try
        {
            started = process.Start();
            if (!started) throw new JavaException(JavaErrorCode.ExecutionFailed, "无法启动 Java 检测进程。");
            var stdout = CaptureAsync(process.StandardOutput, outputLimit, lifetime);
            var stderr = CaptureAsync(process.StandardError, outputLimit, lifetime);
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(lifetime.Token)).ConfigureAwait(false);
            return new(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new JavaException(JavaErrorCode.ProbeTimeout, "Java 检测超时。");
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or IOException or NotSupportedException)
        { throw new JavaException(JavaErrorCode.ExecutionFailed, "Java 检测进程执行失败。"); }
        finally
        {
            if (started && !process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (Exception e) when (e is Win32Exception or InvalidOperationException or NotSupportedException) { }
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                try { await process.WaitForExitAsync(cleanup.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static async Task<string> CaptureAsync(StreamReader reader, int limit, CancellationTokenSource lifetime)
    {
        try
        {
            var text = new StringBuilder();
            var buffer = new char[2048];
            int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory(), lifetime.Token).ConfigureAwait(false)) > 0)
            {
                if (text.Length + read > limit)
                    throw new JavaException(JavaErrorCode.OutputLimitExceeded, "Java 检测输出超出限制。");
                text.Append(buffer, 0, read);
            }
            return text.ToString();
        }
        catch { lifetime.Cancel(); throw; }
    }
}
