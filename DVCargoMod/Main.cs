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
		if (settings.loggingLevel != LoggingLevel.None && level <= settings.loggingLevel)
		{
			mod?.Logger.Log(message());
		}
	}
}

#region Patches
[HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.RecalculateCaches))]
[HarmonyPriority(Priority.First)]
class DVObjectModel_RecalculateCaches_Patch
{
	static void Postfix(
		ref DVObjectModel __instance,
		ref Dictionary<TrainCarType_v2, List<CargoType_v2>> ____carTypeToLoadableCargo,
		ref Dictionary<CargoType_v2, List<TrainCarType_v2>> ____cargoToLoadableCarTypes
	)
	{
		Main.DebugLog(() => "Recalculating caches");
		// link liveries to traincartypes
		UpdateTrainCars(ref __instance);

		// link traincartypes to cargos
		UpdateCargos(ref __instance);

		// recalculate dicts
		Main.DebugLog(() => "Reloading mappings");
		var cargos = __instance.cargos;
		var carTypes = __instance.carTypes;

		Main.DebugLog(LoggingLevel.Verbose, () => "Reloading _carTypeToLoadableCargo");
		____carTypeToLoadableCargo = __instance.carTypes.ToDictionary(c => c, c => cargos.Where(cg => cg.loadableCarTypes.Any(lct => lct.carType == c)).ToList());
		Main.DebugLog(LoggingLevel.Verbose, () => "Reloading _cargoToLoadableCarTypes");
		____cargoToLoadableCarTypes = cargos.ToDictionary(c => c, c => c.loadableCarTypes.Select(lct => lct.carType).ToList());
		Main.DebugLog(() => "Finished reloading mappings");

		if (Main.settings.loggingLevel > LoggingLevel.None)
		{
			foreach (var cargo in cargos)
			{
				Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} carTypes: [{cargo.loadableCarTypes.Select(info => info.carType.id).Join()}]");
				Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} liveries: [{cargo.loadableCarTypes.SelectMany(info => info.carType.liveries).Select(l => l.id).Distinct().Join()}]");
				Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} prefabs:  [{carTypes.SelectMany(ct => { var gos = cargo.GetCargoPrefabsForCarType(ct); return gos == null ? new List<GameObject>() : gos.ToList(); }).Select(go => go.name).Join()}]");
			}
		}
		Main.DebugLog(() => "Caches recalculated");
	}

	private static void UpdateTrainCars(ref DVObjectModel instance)
	{
		Main.DebugLog(() => "Adding TrainCarLiveries to TrainCarTypes");
		var carTypes = new List<TrainCarType_v2>();
		foreach (var carType in instance.carTypes)
		{
			if (carType.id.Contains("Tank"))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding tankers to {carType.id}");
				carType.liveries.AddRange(Cars.tankers.ConvertAll(t => t.ToV2()));
			}
			else if (!carType.id.Contains("Military") && carType.id.Contains("Boxcar") || carType.id.Contains("Refrigerator"))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding boxcars to {carType.id}");
				carType.liveries.AddRange(Cars.boxcars.ConvertAll(t => t.ToV2()));
			}
			else if (!carType.id.Contains("Military") && carType.id == "Flatbed")
			{
				carType.liveries.AddRange(Cars.boxcars.ConvertAll(t => t.ToV2()));
			}
			carType.liveries = carType.liveries.Distinct().ToList();
			carTypes.Add(carType);
		}
		instance.carTypes = carTypes;
		Main.DebugLog(() => "Completed adding TrainCarLiveryies to TrainCarTypes");
	}

	private static void UpdateCargos(ref DVObjectModel instance)
	{
		Main.DebugLog(() => "Adding LoadableInfos to CargoType_v2s");
		var cargos = new List<CargoType_v2>();
		foreach (var cargo in instance.cargos)
		{
			// add fluids and gasses to tank carks
			if (Cargos.tankCarCargos.Contains(cargo.v1))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding tankers to {cargo.v1}");
				var tankerLiveries = Cars.tankers;
				var tankerTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => tankerLiveries.Contains(l.v1)));

				var tankerPrefab = new GameObject[] { };
				if (TCCDP.IsCargoFlammable(cargo.v1))
				{
					tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankOil);
					tankerPrefab = LoadableInfos.Tankers.TankFlammable;
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

			// add (certain) flatcar cargoes to boxcars
			if (Cargos.boxcarCargoes.Contains(cargo.v1))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding boxcars to {cargo.v1}");
				var boxcarLiveries = Cars.boxcars;
				var boxcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => boxcarLiveries.Contains(l.v1)));
				var boxcarPrefab = cargo.GetCargoPrefabsForCarType(TCT.Boxcar);
				var boxcarInfo = boxcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, boxcarPrefab));

				var loadables = cargo.loadableCarTypes.ToList();
				loadables.AddRange(boxcarInfo);
				cargo.loadableCarTypes = loadables.Distinct().ToArray();
			}

			// add some containerizable cargoes to flat cars in containers
			if (Cargos.containerizableCargos.Contains(cargo.v1))
			{
				Main.DebugLog(LoggingLevel.Verbose, () => $"Adding flatcars to containerizable cargo {cargo.v1}");
				var flatcarLiveries = Cars.flatcars;
				var flatcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => flatcarLiveries.Contains(l.v1)));
				var flatcarPrefab = LoadableInfos.Containers.AllNormalContainers;
				if (Cargos.containerizableCargosIsoOxydizing.Contains(cargo.v1))
				{
					flatcarPrefab = LoadableInfos.Containers.Hazmat.Oxydizing;
				}
				else if (Cargos.containerizableCargosIsoExplosive.Contains(cargo.v1))
				{
					flatcarPrefab = LoadableInfos.Containers.Hazmat.Explosive;
				}
				var flatcarInfo = flatcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, flatcarPrefab));
				var loadables = cargo.loadableCarTypes.ToList();
				loadables.AddRange(flatcarInfo);
				cargo.loadableCarTypes = loadables.Distinct().ToArray();
			}
			cargos.Add(cargo);
		}
		instance.cargos = cargos;
		Main.DebugLog(() => "Completed LoadableInfos to CargoType_v2s");
	}
}
#endregion