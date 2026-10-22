using System.Security.Cryptography;
using System.Text;
using FMCore.Utilities;

namespace FMCore.Instances;

// Lock files live outside game directories and are never deleted: replacing their inode would split mutual exclusion on Unix.
internal static class InstanceLock
{
    internal static FileStream Acquire(string root, string relative)
    {
        var path = LockPath(root, relative);
        GameFiles.CreateDirectory(Path.GetDirectoryName(path)!);
        return Open(path, FileMode.OpenOrCreate);
    }

    internal static FileStream? AcquireLegacy(string gameDirectory)
    {
        var path = GameFiles.Resolve(gameDirectory, ".fmcore/instance.lock");
        try { return Open(path, FileMode.Open); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    internal static bool IsInUse(string root, string relative)
    {
        try
        {
            try { using var shared = Open(LockPath(root, relative), FileMode.Open); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            var game = relative.Length == 0 ? root : GameFiles.Resolve(root, relative);
            using var legacy = AcquireLegacy(game);
            return false;
        }
        catch (InstanceException e) when (e.Code == InstanceErrorCode.Busy) { return true; }
    }

    private static string LockPath(string root, string relative)
    {
        var game = relative.Length == 0 ? root : GameFiles.Resolve(root, relative);
        game = Path.GetRelativePath(root, game).Replace('\\', '/');
        if (OperatingSystem.IsWindows()) game = game.ToUpperInvariant();
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(game)));
        return GameFiles.Resolve(root, $".fmcore/instance-locks/{key}.lock");
    }

    private static FileStream Open(string path, FileMode mode)
    {
        try { return new(path, mode, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) when ((e.HResult & 0xffff) is 32 or 33 or 11)
        { throw new InstanceException(InstanceErrorCode.Busy, "此实例正在启动、运行或被另一个操作使用。"); }
    }
}
