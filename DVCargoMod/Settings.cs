using UnityModManagerNet;

namespace DvCargoMod;

public enum LoggingLevel
{
    None,
    Minimal,
    Verbose,
    Debug,
}

public class Settings : UnityModManager.ModSettings, IDrawable
{
    public readonly string? version = Main.mod?.Info.Version;

    [Draw("Logging level")]
    public LoggingLevel loggingLevel = LoggingLevel.None;

    public override void Save(UnityModManager.ModEntry entry)
    {
        Save(this, entry);
    }

    public void OnChange() { }
}

