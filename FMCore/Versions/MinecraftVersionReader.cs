namespace FMCore.Versions;

public class MinecraftVersionReader
{
    public string[] GetVersionDirectories(string minecraftRoot)
    {
        string versionsPath = Path.Combine(minecraftRoot, "versions");

        if (!Directory.Exists(versionsPath))
        {
            return [];
        }

        return Directory.GetDirectories(versionsPath);
    }
}
