from pathlib import Path
import json
from .models import MinecraftVersion
import os



def _find_minecraft_roots(
    minecraft_root: str | Path | None,
) -> list[Path]:
    if minecraft_root is not None:
        if isinstance(minecraft_root, str) and not minecraft_root.strip():
            raise ValueError("Minecraft 根目录不能为空字符串")

        root = Path(minecraft_root).expanduser().resolve()

        if root.exists() and not root.is_dir():
            raise NotADirectoryError(f"Minecraft 根路径不是目录：{root}")

        return [root]

    candidates = [Path.cwd() / ".minecraft"]

    appdata = os.environ.get("APPDATA")
    if appdata:
        candidates.append(Path(appdata) / ".minecraft")

    found = []

    for candidate in candidates:
        if candidate.is_dir():
            root = candidate.resolve()

            if root not in found:
                found.append(root)

    return found


def get_version_directories(
    minecraft_root: str | Path | None = None,
) -> list[Path]:
    directories = []

    for root in _find_minecraft_roots(minecraft_root):
        versions_path = root / "versions"

        if not versions_path.exists():
            continue

        for path in versions_path.iterdir():
            if not path.is_dir():
                continue

            json_path = path / f"{path.name}.json"

            if json_path.is_file():
                directories.append(path)

    return directories

def read_version_json(version_directory: str | Path) -> dict:
    directory = Path(version_directory)
    json_path = directory / f"{directory.name}.json"

    text = json_path.read_text(encoding="utf-8")
    data = json.loads(text)

    if not isinstance(data, dict):
        raise ValueError(f"版本 JSON 的最外层必须是对象：{json_path}")

    version_id = data.get("id")

    if not isinstance(version_id, str) or not version_id.strip():
        raise ValueError(f"版本 JSON 缺少有效的 id：{json_path}")

    return data
def get_versions(minecraft_root: str | Path | None = None,) -> list[MinecraftVersion]:
    versions = []

    for directory in get_version_directories(minecraft_root):
        data = read_version_json(directory)

        version_type = data.get("type")

        inherits_from = data.get("inheritsFrom")

        main_class = data.get("mainClass")

        if inherits_from is not None:
            if not isinstance(inherits_from, str) or not inherits_from.strip():
                raise ValueError(f"父版本 ID 必须是非空字符串：{directory}")

        if version_type is not None and not isinstance(version_type, str):
            raise ValueError(f"版本 type 必须是字符串：{directory}")


        if main_class is not None:
            if not isinstance(main_class, str) or not main_class.strip():
                raise ValueError(f"启动入口必须是非空字符串：{directory}")

        version = MinecraftVersion(
            id=data["id"],
            version_type=version_type,
            directory=directory,
            inherits_from=inherits_from,
            main_class=main_class,
        )

        versions.append(version)

    return versions

def get_parent_version(
    version: MinecraftVersion,
    versions: list[MinecraftVersion],
) -> MinecraftVersion | None:
    parent_id = version.inherits_from

    if parent_id is None:
        return None

    minecraft_root = version.directory.parent.parent.resolve()
    matches = []

    for candidate in versions:
        candidate_root = candidate.directory.parent.parent.resolve()

        if candidate.id == parent_id and candidate_root == minecraft_root:
            matches.append(candidate)

    if not matches:
        raise LookupError(f"版本 {version.id} 缺少父版本：{parent_id}")

    if len(matches) > 1:
        raise ValueError(f"父版本 ID 存在重名冲突：{parent_id}")

    return matches[0]

def get_inheritance_chain(
    version: MinecraftVersion,
    versions: list[MinecraftVersion],
) -> list[MinecraftVersion]:
    chain = []
    visited_ids = set()
    current = version

    while current is not None:
        if current.id in visited_ids:
            raise ValueError(f"版本继承存在循环：再次遇到 {current.id}")

        visited_ids.add(current.id)
        chain.append(current)

        current = get_parent_version(current, versions)

    return chain

def resolve_main_class(
    version: MinecraftVersion,
    versions: list[MinecraftVersion],
) -> str:
    chain = get_inheritance_chain(version, versions)

    for item in chain:
        if item.main_class is not None:
            return item.main_class

    raise ValueError(
        f"版本 {version.id} 及其父版本均未声明启动入口 mainClass"
    )

def get_version(minecraft_root: str | Path | None = None,version_id: str | None = None,) -> MinecraftVersion:
    if not isinstance(version_id, str) or not version_id.strip():
        raise ValueError("版本 ID 必须是非空字符串")

    versions = get_versions(minecraft_root)
    matches = []

    for version in versions:
        if version.id == version_id:
            matches.append(version)

    if not matches:
        raise LookupError(f"未找到版本：{version_id}")

    if len(matches) > 1:
        raise ValueError(f"版本 ID 存在重名冲突：{version_id}")

    return matches[0]
