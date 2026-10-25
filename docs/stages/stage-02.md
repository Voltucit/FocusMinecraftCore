# 第 2 阶段：启动已有原版游戏

状态：进行中。已将当前 Python 实现迁至 C#，阶段尚未结束。

## 已有功能

- JavaReader.GetJavaInfoAsync：指定程序的完整版本、主版本和可选架构。
- JavaReader.GetJavasAsync：环境变量、.jdks、JavaSoft 和额外目录发现，失败候选诊断和继续。
- OfflineAccount.Create：稳定的 Minecraft 离线 UUID。
- LaunchConfig：显式版本、Java、离线身份、内存和默认/自定义游戏目录。
- 内部 LibraryResolver：当前版本依赖声明、action 与 os.name 的有序规则、常见 Maven 坐标、普通 artifact 路径优先与后备计算。
- 内部 PathSafety：路径规范化、Windows 保留名称、已存在链接目标解析与 libraries 边界检查。

只有明确 natives 映射与 classifiers 的无普通 artifact 条目才留给原生库步骤。旧格式原生库、os.arch/os.version/features 规则尚未支持，明确报错。缺失文件的路径定位不代表已下载、校验或可启动。

## 下一步

1. 根据所选 Java 的实际架构选择原生库，处理未知架构并安全解压。
2. 完成依赖继承合并、去重、classpath 与客户端 JAR 定位。
3. 处理 assets、日志配置、旧版和新版参数、占位符及高级设置。
4. 管理游戏进程、日志、退出状态与停止操作。
5. 使用调用方明确选择的 Java 和完整原版安装进行真实启动验收。
6. 完成阶段接口、中文手册、独立示例和综合启动样例，再宣布阶段完成。

## 验证边界

迁移回归覆盖 Python 已有场景，并增加受控进程的取消、超时、环境隔离和错误输出保护，以及真实临时目录联接点越界检查。本机真实 Java 和已有 1.8.9 文件只读核对与这些测试分别记录；尚未启动 Minecraft，没有真实 x86 或 ARM64 游戏运行证据。
