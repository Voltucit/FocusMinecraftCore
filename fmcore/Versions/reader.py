from pathlib import Path


def get_version_directories(minecraft_root):
    versions_path = Path(minecraft_root) / "Versions"

    if not versions_path.exists():
        return []

    directories = []

    for path in versions_path.iterdir():
        if path.is_dir():
            directories.append(path)

    return directories
