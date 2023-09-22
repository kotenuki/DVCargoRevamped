using System;
using UnityModManagerNet;

namespace DvCargoMod;

public enum LoggingLevel
{
    None = 0,
    Minimal = 1,
    Verbose = 2,
    Debug = 3,
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

