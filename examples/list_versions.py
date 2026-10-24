import argparse
from pathlib import Path
import sys

from fmcore.versions import get_versions


def main() -> int:
    parser = argparse.ArgumentParser(description="查询本地 Minecraft 版本列表")
    parser.add_argument("--minecraft-root", type=Path, help="Minecraft 根目录，省略时自动发现")
    args = parser.parse_args()

    try:
        versions = get_versions(minecraft_root=args.minecraft_root)
    except (OSError, UnicodeError, ValueError) as error:
        print(f"查询失败：{error}", file=sys.stderr)
        return 1

    print(f"读取到 {len(versions)} 个版本。")
    for version in versions:
        print(version.id, version.directory)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
