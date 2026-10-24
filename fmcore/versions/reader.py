from pathlib import Path
import json
from .models import MinecraftVersion


def get_version_directories(minecraft_root: str | Path) -> list[Path]:
    versions_path = Path(minecraft_root) / "versions"

    if not versions_path.exists():
        return []

    directories = []

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
def get_versions(minecraft_root: str | Path) -> list[MinecraftVersion]:
    versions = []

    for directory in get_version_directories(minecraft_root):
        data = read_version_json(directory)

        version_type = data.get("type")

        if version_type is not None and not isinstance(version_type, str):
            raise ValueError(f"版本 type 必须是字符串：{directory}")

        version = MinecraftVersion(
            id=data["id"],
            version_type=version_type,
            directory=directory,
        )

        versions.append(version)

    return versions
