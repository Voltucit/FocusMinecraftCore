# Java 检测素材

这些文本按 OpenJDK/Corretto/Temurin 的 `java -XshowSettings:properties -version` 输出格式手工精简整理；安装目录替换为 `/fixture/...`，不含真实用户名或环境变量。Windows 测试会将该目录替换为测试临时目录。

用于验证属性解析、Java 8 下划线版本、现代版本、预发布标识与架构别名，不代表本机安装了这些 Java。子进程夹具使用测试项目自身的入口，仅验证管道、超时、取消、输出限制和退出码；普通启动继续运行 xUnit。真实 Java 的运行验证单独记录在 `docs/java.md`。
