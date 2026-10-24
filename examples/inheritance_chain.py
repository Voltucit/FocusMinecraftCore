import argparse
from pathlib import Path
import sys

from fmcore.versions import get_inheritance_chain, get_versions


def main() -> int:
    parser = argparse.ArgumentParser(description="查看各本地 Minecraft 版本的继承链")
    parser.add_argument("--minecraft-root", type=Path, help="Minecraft 根目录，省略时自动发现")
    args = parser.parse_args()

    try:
        versions = get_versions(minecraft_root=args.minecraft_root)
    except (OSError, UnicodeError, ValueError) as error:
        print(f"查询失败：{error}", file=sys.stderr)
        return 1

    failed = False
    for version in versions:
        try:
            chain = get_inheritance_chain(version, versions)
        except (LookupError, ValueError) as error:
            print(f"{version.id} [{version.directory}]：{error}", file=sys.stderr)
            failed = True
            continue

        ids = [item.id for item in chain]
        print(f"{version.id} [{version.directory}]：{' -> '.join(ids)}")

    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
