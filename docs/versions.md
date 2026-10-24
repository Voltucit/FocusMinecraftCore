# 版本模块使用手册

本手册对应 FMCore 2.0 第 1 阶段的基础本地版本读取能力。使用 Python 3.13，在源码项目根目录运行示例；当前尚未提供 Python 安装包。

## 公开入口

```python
from fmcore.versions import (
    MinecraftVersion,
    get_versions,
    get_version,
    get_inheritance_chain,
    resolve_main_class,
)
```

普通游戏列表使用 `get_versions()`，指定版本查询使用 `get_version()`。继承链与入口解析供需要诊断或进一步准备启动的调用方使用。

## 查询版本列表

最简用法：

```python
from fmcore.versions import get_versions

for version in get_versions():
    print(version.id, version.directory)
```

显式指定 Minecraft 根目录：

```python
from pathlib import Path
from fmcore.versions import get_versions

root = Path(".minecraft")
versions = get_versions(minecraft_root=root)

for version in versions:
    print(version.id, version.version_type, version.directory)
```

参数应指向 Minecraft 根目录，即包含 `versions` 的目录。显式指定的路径可以使用其他文件夹名称，不要求必须叫 `.minecraft`。

未传入根目录时，只检查：

1. 当前工作目录的 `.minecraft`。
2. Windows `%APPDATA%` 的 `.minecraft`；变量未设置时跳过这项。

当前工作目录由程序运行环境决定，可通过 `Path.cwd()` 查看。不会递归查找任意磁盘位置，也不会自动进入测试资料子目录。

两处都存在时汇总两处的版本；解析后相同的根目录仅扫描一次。相同 ID 出现在不同版本目录时，列表保留每一项，可使用 `directory` 区分。列表顺序没有保证。

没有找到根目录或 `versions` 时返回空列表，不创建目录。空版本目录和不包含同名 JSON 的目录被跳过。目录中没有同名 JSON 时不会猜测其他 JSON 文件。

## 查询指定版本

自动发现根目录并按 JSON ID 查询：

```python
from fmcore.versions import get_version

version = get_version(version_id="1.21.1")
print(version.id, version.directory)
```

该调用要求搜索范围内存在唯一匹配的 `1.21.1`。明确指定根目录可以缩小查找范围：

```python
from pathlib import Path
from fmcore.versions import get_version

version = get_version(
    minecraft_root=Path(".minecraft"),
    version_id="1.21.1",
)
print(version.directory)
```

为保留原有位置参数顺序，函数签名的第一个参数仍是 `minecraft_root`。自动查询单个版本时请写 `version_id=...`；`get_version("1.21.1")` 会被解释为传入根目录，并因缺少 ID 而报错。

未找到匹配 ID 时抛出 `LookupError`；存在多个匹配时抛出 `ValueError`。当前实现复用完整列表读取，因此搜索范围中的其他损坏 JSON 也可能使这次查询失败。

如果已经通过 `get_versions()` 拿到了用户选择的版本对象，可以直接保留并使用它，无需再次查询。

## 返回模型 MinecraftVersion

| 属性 | 类型 | 含义 |
| --- | --- | --- |
| `id` | `str` | JSON 的版本 ID，与实际目录名分别保存 |
| `version_type` | `str | None` | 自身 JSON 的 `type`，缺失时为 `None` |
| `directory` | `Path` | 发现版本时使用的绝对版本目录路径 |
| `inherits_from` | `str | None` | 自身 JSON 的 `inheritsFrom`，缺失时为 `None` |
| `main_class` | `str | None` | 自身 JSON 的 `mainClass`，不是继承后的有效入口 |

该数据类只承载数据。手动创建对象不会触发磁盘读取或自动校验；文件读取函数负责当前已实现的字段检查。

版本目录目前按下列结构读取：

```text
Minecraft根目录/
└── versions/
    └── custom-folder/
        └── custom-folder.json
```

其中 JSON 的 `id` 可以不同于 `custom-folder`。JSON 文件名仍须与所在目录名一致。

## 获取继承链

```python
from pathlib import Path
from fmcore.versions import get_versions, get_inheritance_chain

versions = get_versions(minecraft_root=Path(".minecraft"))

for version in versions:
    chain = get_inheritance_chain(version, versions)
    print(version.id, [item.id for item in chain])
```

链的顺序为当前版本、直接父版本、再上层父版本，包含当前版本本身。没有父版本时，结果只包含当前版本。

`versions` 应包含当前版本和它在同一 Minecraft 根目录内需要的全部父版本。多根目录汇总列表可以作为输入，查找会按版本目录限定所属根目录，不跨根目录借用父版本。

父版本缺失抛出 `LookupError`；同一根目录内父版本 ID 重名或存在循环继承时抛出 `ValueError`。当前仅定位关系，不合并完整启动配置。

## 解析最终启动入口

```python
from pathlib import Path
from fmcore.versions import get_versions, resolve_main_class

versions = get_versions(minecraft_root=Path(".minecraft"))

for version in versions:
    main_class = resolve_main_class(version, versions)
    print(version.id, main_class)
```

函数先验证完整继承链，再从当前版本开始查找最近的 `mainClass` 声明，返回字符串。子版本声明优先于父版本。

即使当前版本自己有入口，缺失父版本或循环仍会报错。整个继承链都没有入口时抛出 `ValueError`。查询列表时不要求版本已经具备启动入口。

## 错误处理

单个查询的错误处理示例：

```python
from fmcore.versions import get_version

try:
    version = get_version(version_id="1.21.1")
except (OSError, UnicodeError, ValueError, LookupError) as error:
    print(f"无法查询版本：{error}")
else:
    print(version.id, version.directory)
```

- `OSError` 及其子类：目录、文件访问或权限失败；明确传入的根路径是文件时为 `NotADirectoryError`。
- `UnicodeError`：版本 JSON 的 UTF-8 解码失败。
- `ValueError`：无效参数、无效字段、JSON 语法错误、重名、循环或缺少启动入口等；`json.JSONDecodeError` 是其子类。
- `LookupError`：指定版本或父版本缺失。

当前采用严格读取：发现候选版本的损坏 JSON 或无效字段时，列表查询直接报错，不静默忽略，也不返回部分成功列表。错误展示与是否继续其他操作，由调用方决定。

## 验证范围与限制

- 已有回归覆盖：三层继承、循环、父版本缺失和重名、目录名与 JSON ID 不同、多根目录汇总、重复 ID 保留、显式根目录选择。
- 已读取本地 `1.21.1`、`1.8.9`、`26.4-snapshot-2` 版本元数据；这只证明当前字段可读。
- 尚未实现完整版本元数据合并、依赖补全、Java 选择、账号登录或游戏启动。
- 未按加载器名称过滤版本，也未完成各加载器真实游戏兼容测试。
- 当前版本对象不是安全审计或可启动性保证。启动阶段仍需校验依赖、参数和路径。
- 当前 API 为同步只读查询，没有进度、取消或异步接口。

独立可运行程序及命令见 [examples/README.md](../examples/README.md)。
