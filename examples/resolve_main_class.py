import argparse
from pathlib import Path
import sys

from fmcore.versions import get_versions, resolve_main_class


def main() -> int:
    parser = argparse.ArgumentParser(description="解析各本地 Minecraft 版本的最终启动入口")
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
            main_class = resolve_main_class(version, versions)
        except (LookupError, ValueError) as error:
            print(f"{version.id} [{version.directory}]：{error}", file=sys.stderr)
            failed = True
            continue

        print(f"{version.id} [{version.directory}]：{main_class}")

    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
