namespace FMCore.Java;

/// <summary>所选 Java 的路径、完整版本、主版本和可选架构（x86、x64、arm64）。</summary>
public sealed record JavaInfo(string Executable, string Version, int MajorVersion, string? Architecture = null);
