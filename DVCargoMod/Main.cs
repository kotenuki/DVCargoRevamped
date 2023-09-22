using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;

using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

using TCCDP = TrainCarAndCargoDamageProperties;

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
		// log setting level is not None if higher than None, and the given level is equal or higher than the setting level
		if (settings.loggingLevel != LoggingLevel.None && level >= settings.loggingLevel)
		{
			mod?.Logger.Log(message());
		}
	}
}

#region Patches
[HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.RecalculateCaches))]
[HarmonyPatch(Priority.Last)]
class DVObjectModel_RecalculateCaches_Patch
{
	static void Postfix(
		ref DVObjectModel __instance,
		ref Dictionary<TrainCarType_v2, List<CargoType_v2>> ____carTypeToLoadableCargo,
		ref Dictionary<CargoType_v2, List<TrainCarType_v2>> ____cargoToLoadableCarTypes
	)
	{
		Main.DebugLog(() => "Recalculating caches...");
		// link liveries to traincartypes
		UpdateTrainCars(ref __instance);

		// link traincartypes to cargos
		UpdateCargos(ref __instance);

		// recalculate dicts
		Main.DebugLog(() => "Recalculating _carTypeToLoadableCargo and _cargoToLoadableCarTypes dictionaries...");
		var cargos = __instance.cargos;
		____carTypeToLoadableCargo = __instance.carTypes.ToDictionary(
			(TrainCarType_v2 c) => c,
			(TrainCarType_v2 c) => cargos.Where(
				(CargoType_v2 cg) => cg.loadableCarTypes.Any(
					(CargoType_v2.LoadableInfo lct) => lct.carType == c)).ToList());
		____cargoToLoadableCarTypes = cargos.ToDictionary(
			(CargoType_v2 c) => c,
			(CargoType_v2 c) => c.loadableCarTypes.Select(
				(CargoType_v2.LoadableInfo lct) => lct.carType).ToList());
		Main.DebugLog(() => "Finished recalculating _carTypeToLoadableCargo and _cargoToLoadableCarTypes dictionaries");

		if (Main.settings.loggingLevel > LoggingLevel.None)
		{
			foreach (var cargo in cargos)
			{
				Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} carTypes: [{cargo.loadableCarTypes.Select(info => info.carType.id).Join(delimiter: ",")}]");
				Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} liveries: [{cargo.loadableCarTypes.SelectMany(info => info.carType.liveries).Select(l => l.id).Distinct().Join(delimiter: ",")}]");
				// var prefabInfo = cargo.loadableCarTypes
				// 	.Select(info => $"{{\"{info.carType.id}\": [{info.cargoPrefabVariants.Select(prefab => $"\"{prefab.name}\"").Join()}]}}")
				// 	.Join();
				// Debug.Log(Main.PREFIX + $"\"{cargo.id}\": [{prefabInfo}],");
			}
		}
		Main.DebugLog(() => "Caches recalculated");
	}

	private static void UpdateTrainCars(ref DVObjectModel instance)
	{
		Main.DebugLog(() => "Adding TrainCarLiveries to TrainCarTypes...");
		var carTypes = new List<TrainCarType_v2>();
		foreach (var carType in instance.carTypes)
		{
			if (carType.id.Contains("Tank"))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding tankers to {carType.id}...");
				carType.liveries.AddRange(Cars.tankers.ConvertAll(t => t.ToV2()));
			}
			else if (!carType.id.Contains("Military") && carType.id.Contains("Boxcar") || carType.id.Contains("Refrigerator"))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding boxcars to {carType.id}...");
				carType.liveries.AddRange(Cars.boxcars.ConvertAll(t => t.ToV2()));
			}
			// else if (!carType.id.Contains("Military") && carType.id.Contains("Flatbed"))
			// {
			// 	carType.liveries.AddRange(Cars.boxcars.ConvertAll(t => t.ToV2()));
			// }
			carType.liveries = carType.liveries.Distinct().ToList();
			carTypes.Add(carType);
		}
		instance.carTypes = carTypes;
		Main.DebugLog(() => "Completed adding TrainCarLiveryies to TrainCarTypes");
	}

	private static void UpdateCargos(ref DVObjectModel instance)
	{
		Main.DebugLog(() => "Adding LoadableInfos to CargoType_v2s...");
		var cargos = new List<CargoType_v2>();
		foreach (var cargo in instance.cargos)
		{
			if (Cargos.tankCarCargos.Contains(cargo.v1))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding tankers to {cargo.v1}");
				var tankerLiveries = Cars.tankers;
				var tankerTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => tankerLiveries.Contains(l.v1)));

				var tankerPrefab = new GameObject[] { };
				if (TCCDP.IsCargoFlammable(cargo.v1))
				{
					tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankOil);
				}
				else if (TCCDP.IsCargoExplosive(cargo.v1))
				{
					tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankGas);
				}
				else if (TCCDP.IsCargoCorrosiveLiquid(cargo.v1))
				{
					tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankChem);
				}
				var tankerInfo = tankerTypes.Select(t => new CargoType_v2.LoadableInfo(t, tankerPrefab));
				var loadables = cargo.loadableCarTypes.ToList();
				loadables.AddRange(tankerInfo);
				cargo.loadableCarTypes = loadables.Distinct().ToArray();
			}
			if (Cargos.boxcarCargoes.Contains(cargo.v1))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding boxcars to {cargo.v1}");
				var boxcarLiveries = Cars.boxcars;
				var boxcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => boxcarLiveries.Contains(l.v1)));
				var boxcarPrefab = cargo.GetCargoPrefabsForCarType(TCT.Boxcar);
				var boxcarInfo = boxcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, boxcarPrefab));

				// var flatcarLiveries = Cars.flatcars;
				// var flatcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => flatcarLiveries.Contains(l.v1)));
				// var flatcarPrefab = cargo.GetCargoPrefabsForCarType(Cars.flatcars[0].ToV2().parentType);
				// var flatcarInfo = flatcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, flatcarPrefab));

				var loadables = cargo.loadableCarTypes.ToList();
				loadables.AddRange(boxcarInfo);
				// loadables.AddRange(flatcarInfo);
				cargo.loadableCarTypes = loadables.Distinct().ToArray();
			}
			cargos.Add(cargo);
		}
		instance.cargos = cargos;
		Main.DebugLog(() => "Completed LoadableInfos to CargoType_v2s");
	}
}
#endregion