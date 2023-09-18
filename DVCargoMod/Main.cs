using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using static DV.ThingTypes.CargoType;
using static DV.ThingTypes.TrainCarType;

namespace DvCargoMod;

public static class Main
{
	public static UnityModManager.ModEntry? mod;
	public const string PREFIX = "[DvCargoMod] ";

	public const bool SKIP_ORIGINAL = false;
	public const bool KEEP_ORIGINAL = true;
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		Harmony? harmony = null;
		mod = modEntry;

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
}

[HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.RecalculateCaches))]
class DVObjectModel_RecalculateCaches_Patch
{
	static void Postfix(
		ref DVObjectModel __instance,
		ref Dictionary<TrainCarType_v2, List<CargoType_v2>> ____carTypeToLoadableCargo,
		ref Dictionary<CargoType_v2, List<TrainCarType_v2>> ____cargoToLoadableCarTypes
	)
	{
		// link liveries to traincartypes
		updateTrainCars(ref __instance);

		// link traincartypes to cargos
		updateCargos(ref __instance);

		// recalculate dicts
		// Debug.Log("recalculate dicts");
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

		// foreach (var cargo in cargos)
		// {
		// Debug.Log(Main.PREFIX + $"{cargo.id}: [{cargo.loadableCarTypes.Select(info => info.carType.id).Join(delimiter: ",")}]");
		// Debug.Log(Main.PREFIX + $"{cargo.id}: [{cargo.loadableCarTypes.SelectMany(info => info.carType.liveries).Select(l => l.id).Join(delimiter: ",")}]");

		// var prefabInfo = cargo.loadableCarTypes
		// 	.Select(info => $"{{\"{info.carType.id}\": [{info.cargoPrefabVariants.Select(prefab => $"\"{prefab.name}\"").Join()}]}}")
		// 	.Join();
		// Debug.Log(Main.PREFIX + $"\"{cargo.id}\": [{prefabInfo}],");
		// }


	}

	private static void updateTrainCars(ref DVObjectModel instance)
	{
		var carTypes = new List<TrainCarType_v2>();
		foreach (var carType in instance.carTypes)
		{
			if (carType.id.Contains("Tank"))
			{
				carType.liveries.AddRange(Cars.tankers.ConvertAll(t => t.ToV2()));
			}
			else if (!carType.id.Contains("Military") && carType.id.Contains("Boxcar") || carType.id.Contains("Refrigerator"))
			{
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
	}

	private static void updateCargos(ref DVObjectModel instance)
	{
		var cargos = new List<CargoType_v2>();
		foreach (var cargo in instance.cargos)
		{
			if (Cargos.tankCarCargos.Contains(cargo.v1))
			{
				var tankerLiveries = Cars.tankers;
				var tankerTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => tankerLiveries.Contains(l.v1)));
				var tankerPrefab = cargo.GetCargoPrefabsForCarType(Cars.tankers[0].ToV2().parentType);
				var tankerInfo = tankerTypes.Select(t => new CargoType_v2.LoadableInfo(t, tankerPrefab));
				var loadables = cargo.loadableCarTypes.ToList();
				loadables.AddRange(tankerInfo);
				cargo.loadableCarTypes = loadables.Distinct().ToArray();
			}
			if (Cargos.boxcarCargoes.Contains(cargo.v1))
			{
				var boxcarLiveries = Cars.boxcars;
				var boxcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => boxcarLiveries.Contains(l.v1)));
				var boxcarPrefab = cargo.GetCargoPrefabsForCarType(Cars.boxcars[0].ToV2().parentType);
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
	}
}

static class Cargos
{
	public static List<CargoType> tankCarCargos = new List<CargoType>
	{
		CrudeOil, Diesel, Gasoline,
		Methane, Alcohol,
		Ammonia, SodiumHydroxide,
		Argon, Nitrogen, CryoHydrogen, CryoOxygen,
		ChemicalsIskar, ChemicalsSperex,
	};

	public static List<CargoType> boxcarCargoes = new List<CargoType>
	{
		Boards, Plywood,
		SteelBentPlates, SteelBillets, SteelRails, SteelRolls, SteelSlabs,
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts, Medicine,
	};

	public static List<CargoType> containerizedCargos = new List<CargoType>
	{
		ElectronicsAAG, ElectronicsIskar, ElectronicsKrugmann, ElectronicsNovae, ElectronicsTraeg,
		ClothingNeoGamma, ClothingNovae, ClothingObco, ClothingTraeg,
		ToolsIskar, ToolsBrohm, ToolsAAG, ToolsNovae, ToolsTraeg,
		ChemicalsIskar, ChemicalsSperex,
		Argon, Nitrogen, CryoHydrogen, CryoOxygen,
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts, Medicine,
		EmptySunOmni, EmptyIskar, EmptyObco, EmptyGoorsk, EmptyKrugmann, EmptyBrohm,
		EmptyAAG, EmptySperex, EmptyNovae, EmptyTraeg, EmptyChemlek, EmptyNeoGamma,
	};

	public static List<CargoType> perishableCargos = new List<CargoType>
	{
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
		Chickens, Cows, Goats, Pigs, Sheep,
	};

	public static List<CargoType> bulkCargos = new List<CargoType>
	{
		CargoType.Coal, IronOre,
		Logs,
		ScrapMetal
	};
}

static class Cars
{
	public static List<TrainCarType_v2> fluidCars = new List<TrainCarType>
	{
		// FlatbedEmpty,
		TankWhite, // TankYellow, TankChrome, // TankOil
		TankBlue, // TankOrange,  // TankGas
		TankBlack, // TankChem
	}.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

	public static List<TrainCarType_v2> nonPerishableCars = new List<TrainCarType>
	{
		FlatbedEmpty,
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		GondolaGray, GondolaGreen, GondolaRed,
	}.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

	public static List<TrainCarType_v2> perishableCars = new List<TrainCarType>
	{
		FlatbedEmpty,
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		RefrigeratorWhite,
	}.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

	public static List<TrainCarType_v2> bulkCars = new List<TrainCarType>
	{
		HopperBrown, HopperTeal, HopperYellow,
		GondolaGray, GondolaGreen, GondolaRed,
	}.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

	public static List<TrainCarType> tankers = new List<TrainCarType>
	{
		TankWhite, TankYellow, TankChrome,
		TankBlue, TankOrange,
		TankBlack,
	};

	public static List<TrainCarType> boxcars = new List<TrainCarType>
	{
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		RefrigeratorWhite
	};

	public static List<TrainCarType> flatcars = new List<TrainCarType>
	{
		FlatbedEmpty
	};
}

static class LoadableInfos
{
	public static GameObject[] tankFlammable = CargoType.CrudeOil.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] tankExplosive = CargoType.Gasoline.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] tankCorrosive = CargoType.Ammonia.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarAsph = CargoType.Argon.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarExplosive = CargoType.CryoHydrogen.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarOxy = CargoType.CryoOxygen.ToV2().loadableCarTypes[0].cargoPrefabVariants;
}