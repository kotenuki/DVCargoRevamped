using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace DvCargoMod;

public static class Main
{
	public static UnityModManager.ModEntry? mod;
	public static Settings settings = new Settings();
	public const bool SKIP_ORIGINAL = false;
	public const bool KEEP_ORIGINAL = true;
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		Harmony? harmony = null;
		mod = modEntry;

		try
		{
			Settings? loaded = Settings.Load<Settings>(modEntry);
			settings = loaded.version == mod.Info.Version ? loaded : new Settings();
		}
		catch
		{
			settings = new Settings();
		}

		mod.OnGUI = settings.Draw;
		mod.OnSaveGUI = settings.Save;

		try
		{
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll(modEntry.Info.Id);
			return false;
		}

		return true;
	}

	public static void DebugLog(Func<string> message)
	{
		DebugLog(LoggingLevel.Minimal, message);
	}
	public static void DebugLog(LoggingLevel level, Func<string> message)
	{
		if (settings.loggingLevel != LoggingLevel.None && level <= settings.loggingLevel)
		{
			Debug.Log(message());
		}
	}

	public static void ErrorLog(Func<string> message)
	{
		Debug.LogError(message());
	}
}
