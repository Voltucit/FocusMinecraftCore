using System.Diagnostics;

namespace FMCore.Launch;

/// <summary>启动后自动加入的多人服务器。SDK 按客户端元数据选择旧版参数或 Quick Play，不执行连接或 DNS 探测。</summary>
[DebuggerDisplay("游戏服务器设置")]
public sealed record GameServerConfig
{
    /// <summary>域名、IPv4 或 IPv6 地址，不含协议、路径或端口；IPv6 可带方括号。国际域名保存为 ASCII。</summary>
    public string Host { get; init; } = "";
    /// <summary>端口，范围 1–65535；默认 25565。</summary>
    public int Port { get; init; } = 25565;
    /// <summary>不在诊断字符串中暴露服务器地址。</summary>
    public override string ToString() => "游戏服务器设置";
}
