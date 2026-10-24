from dataclasses import dataclass
from pathlib import Path


@dataclass
class MinecraftVersion:
    id: str
    version_type: str | None
    directory: Path
    inherits_from: str | None = None
