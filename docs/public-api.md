# 公开 API 清单

此清单对应 Python 重写第 1 阶段。正式导入入口为 `fmcore.versions`；签名中的 `Path` 来自 `pathlib`。

## MinecraftVersion

```text
MinecraftVersion(
    id: str,
    version_type: str | None,
    directory: Path,
    inherits_from: str | None = None,
    main_class: str | None = None,
)
```

公开属性与参数同名。它是可变数据类，构造本身不执行 I/O 或自动校验。

## get_versions

```text
get_versions(
    minecraft_root: str | Path | None = None,
) -> list[MinecraftVersion]
```

列出指定根目录或自动发现的两处根目录中的版本。未发现时返回空列表；不同目录中的同 ID 版本保留。读取失败抛出文件、解码或数据异常。

## get_version

```text
get_version(
    minecraft_root: str | Path | None = None,
    version_id: str | None = None,
) -> MinecraftVersion
```

按 JSON ID 获取唯一版本，`version_id` 在实际调用中必填。自动发现时使用 `get_version(version_id="...")`。显式根目录可消除跨根目录歧义。

无效 ID 或重名为 `ValueError`，无匹配为 `LookupError`；底层读取异常继续传播。

## get_inheritance_chain

```text
get_inheritance_chain(
    version: MinecraftVersion,
    versions: list[MinecraftVersion],
) -> list[MinecraftVersion]
```

返回当前版本到最上层父版本的对象列表。父版本查找限定同一 Minecraft 根目录；缺失为 `LookupError`，重名或循环为 `ValueError`。

## resolve_main_class

```text
resolve_main_class(
    version: MinecraftVersion,
    versions: list[MinecraftVersion],
) -> str
```

验证完整继承链后，以子版本声明优先的顺序确定启动入口。继承关系异常继续传播；整条链缺少入口时为 `ValueError`。

## 内部实现与兼容说明

- `reader.py` 中的扫描、原始 JSON 读取和直接父版本定位函数仍可供现有学习脚本访问，本阶段未删除它们。
- 面向启动器的文档使用 `fmcore.versions` 导出的上述对象和函数，不要求依赖内部文件布局。
- `get_versions()` 的根目录参数由必填变为可选；显式路径调用继续可用。
- `get_version()` 保留原有 `(minecraft_root, version_id)` 位置参数顺序，新增省略根目录时的关键字调用方式。
- 自动发现从最初的单目录选择调整为两处汇总；列表保留重复 ID，单版本查询继续要求唯一匹配。
- 同一根目录中的 JSON ID 与版本文件夹名称分别管理；JSON 文件名仍要求与文件夹同名。
- 新增接口或行为变更时，应同步更新本清单、模块手册、独立示例和相关回归测试。
