from fmcore.Versions.reader import get_version_directories

directories = get_version_directories(
    r"E:\Pychram\FMCore\FMCoreTestsFiles\.minecraft"
)

print(f"找到 {len(directories)} 个候选版本目录。")

for directory in directories:
    print(directory)
