import argparse
from pathlib import Path
import sys

from fmcore.versions import get_version


def main() -> int:
    parser = argparse.ArgumentParser(description="按 ID 查询一个本地 Minecraft 版本")
    parser.add_argument("version_id", help="版本 JSON 中的 ID")
    parser.add_argument("--minecraft-root", type=Path, help="Minecraft 根目录，省略时自动发现")
    args = parser.parse_args()

    try:
        version = get_version(
            minecraft_root=args.minecraft_root,
            version_id=args.version_id,
        )
    except (OSError, UnicodeError, ValueError, LookupError) as error:
        print(f"查询失败：{error}", file=sys.stderr)
        return 1

    print("版本 ID：", version.id)
    print("版本类型：", version.version_type)
    print("版本目录：", version.directory)
    print("父版本 ID：", version.inherits_from)
    print("自身启动入口：", version.main_class)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
