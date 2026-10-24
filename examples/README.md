# 独立调用示例

这些示例分别展示一个功能，使用真实的 `fmcore.versions` 公开接口。当前尚未配置 Python 包分发，因此请在 FMCore 源码项目根目录中执行模块命令。

## 环境

使用 Python 3.13 和项目虚拟环境。Windows PowerShell 中，以下命令假设虚拟环境目录为 `.venv`。

## 查询版本列表

```powershell
.\.venv\Scripts\python.exe -m examples.list_versions
```

自动汇总当前工作目录与 APPDATA 下的 `.minecraft`。明确指定根目录：

```powershell
.\.venv\Scripts\python.exe -m examples.list_versions --minecraft-root "FMCoreTestsFiles/.minecraft"
```

## 查询单个版本

```powershell
.\.venv\Scripts\python.exe -m examples.get_version 1.21.1 --minecraft-root "FMCoreTestsFiles/.minecraft"
```

版本 ID 是必需参数。省略 `--minecraft-root` 时自动发现；重名时提示错误。

## 查看继承链

```powershell
.\.venv\Scripts\python.exe -m examples.inheritance_chain --minecraft-root "FMCoreTestsFiles/.minecraft"
```

对列表中的每个版本独立查看继承链。当前版本与父版本必须属于同一根目录。逐项失败会显示错误，程序继续检查其余项，最终以非零退出码报告存在失败。

## 解析最终启动入口

```powershell
.\.venv\Scripts\python.exe -m examples.resolve_main_class --minecraft-root "FMCoreTestsFiles/.minecraft"
```

对列表中的每个版本独立解析 mainClass，不启动 Java 或游戏。如果测试目录中仍有未声明启动入口的 error-test，这个示例会明确报错并返回 1，属于样本的预期结果。

## 返回码

- `0`：本次查询或所有逐项检查成功；列表为空也属于成功查询。
- `1`：查询失败，或逐项检查中至少有一项失败。
- `2`：命令行参数错误，由 argparse 输出帮助提示。

`--minecraft-root` 参数统一指向包含 `versions` 的 Minecraft 根目录。示例不会创建或修改游戏文件，也不会安装游戏、登录或启动进程。

## 与手动学习脚本的关系

根目录的 checks.py 是本地手动调试脚本，继续保持 Git 忽略。这些独立示例用于公开教学与功能调用验证。
