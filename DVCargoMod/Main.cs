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
		var carTypes = new List<TrainCarType_v2>();
		foreach (var carType in __instance.carTypes)
		{
			if (carType.id.Contains("Tank"))
			{
				// add all tankers
				carType.liveries.AddRange(Cars.tankers.ConvertAll(t => t.ToV2()));
			}
			carType.liveries = carType.liveries.Distinct().ToList();
			carTypes.Add(carType);
		}
		__instance.carTypes = carTypes;

		// link traincartypes to cargoes
		var cargoes = new List<CargoType_v2>();
		foreach (var cargo in __instance.cargos)
		{
			if (Cargoes.tankCarCargoes.Contains(cargo.v1))
			{
				// Debug.Log($"begin {cargo.v1}");
				var tankerLiveries = Cars.tankers;
				// Debug.Log("after tankerLiveries");
				var tankerTypes = __instance.carTypes.Where(carType => carType.liveries.Any(l => tankerLiveries.Contains(l.v1)));
				// Debug.Log("after tankerTypes");
				var tankerPrefab = new GameObject[] { };
				if (TrainCarAndCargoDamageProperties.IsCargoFlammable(cargo.v1))
				{
					tankerPrefab = LoadableInfos.tankFlammable;
				}
				else if (TrainCarAndCargoDamageProperties.IsCargoExplosive(cargo.v1))
				{
					tankerPrefab = LoadableInfos.tankExplosive;
				}
				else if (TrainCarAndCargoDamageProperties.IsCargoCorrosiveLiquid(cargo.v1))
				{
					tankerPrefab = LoadableInfos.tankCorrosive;
				}
				// Debug.Log("after tankerPrefab");
				var tankerInfo = tankerTypes.Select(tct2 => new CargoType_v2.LoadableInfo(tct2, tankerPrefab)).ToArray();
				// Debug.Log("after tankerInfo");
				cargo.loadableCarTypes = tankerInfo;
				// Debug.Log($"finished {cargo.v1}");
			}
			cargoes.Add(cargo);
			// Debug.Log(Main.PREFIX + $"{cargo.id}: [{cargo.loadableCarTypes.Select(info => info.carType.id).Join(delimiter: ",")}]");
			// Debug.Log(Main.PREFIX + $"{cargo.id}: [{cargo.loadableCarTypes.SelectMany(info => info.carType.liveries).Select(l => l.id).Join(delimiter: ",")}]");
		}
		__instance.cargos = cargoes;

		// recalculate dicts
		// Debug.Log("recalculate dicts");
		____carTypeToLoadableCargo = __instance.carTypes.ToDictionary(
			(TrainCarType_v2 c) => c,
			(TrainCarType_v2 c) => cargoes.Where(
				(CargoType_v2 cg) => cg.loadableCarTypes.Any(
					(CargoType_v2.LoadableInfo lct) => lct.carType == c)).ToList());
		____cargoToLoadableCarTypes = cargoes.ToDictionary(
			(CargoType_v2 c) => c,
			(CargoType_v2 c) => c.loadableCarTypes.Select(
				(CargoType_v2.LoadableInfo lct) => lct.carType).ToList());
	}
}

static class Cargoes
{
	public static List<CargoType> tankCarCargoes = new List<CargoType>
	{
		CrudeOil, Diesel, Gasoline,
		Methane, Alcohol,
		Ammonia, SodiumHydroxide,
		// TODO: Argon, Nitrogen, CryoHydrogen, CryoOxygen,
		// TODO: ChemicalsIskar, ChemicalsSperex,
	};

	public static List<CargoType> nonPerishableCargoes = new List<CargoType>
	{
		ElectronicsAAG, ElectronicsIskar, ElectronicsKrugmann, ElectronicsNovae, ElectronicsTraeg, ClothingNeoGamma,
		ClothingNovae, ClothingObco, ClothingTraeg,
		ChemicalsIskar, ChemicalsSperex,
		Boards, Plywood,
		SteelBentPlates, SteelBillets, SteelRails, SteelRolls, SteelSlabs,
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
	};


	public static List<CargoType> perishableCargoes = new List<CargoType>
	{
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
		Chickens, Cows, Goats, Pigs, Sheep,
	};

	public static List<CargoType> bulkCargoes = new List<CargoType>
	{
		CargoType.Coal, IronOre,
		Logs,
		ScrapMetal
	};


}
static class LoadableInfos
{
	public static GameObject[] tankFlammable = CargoType.CrudeOil.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] tankExplosive = CargoType.Gasoline.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] tankCorrosive = CargoType.Ammonia.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarAsph = CargoType.Argon.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarIskar = CargoType.ChemicalsIskar.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarSperex = CargoType.ChemicalsSperex.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarExplosive = CargoType.CryoHydrogen.ToV2().loadableCarTypes[0].cargoPrefabVariants;
	public static GameObject[] flatcarOxy = CargoType.CryoOxygen.ToV2().loadableCarTypes[0].cargoPrefabVariants;
}

static class Cars
{
	public static List<TrainCarType_v2> fluidCars = new List<TrainCarType>
	{
		// FlatbedEmpty,
		TankWhite, // TankYellow, TankChrome, // TankOil
		TankBlue, // TankOrange,  // TankGas
		TankBlack, // TankChem
	}.ConvertAll(c => c.ToV2().parentType).ToHashSet().ToList();

	public static List<TrainCarType_v2> nonPerishableCars = new List<TrainCarType>
	{
		FlatbedEmpty,
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		GondolaGray, GondolaGreen, GondolaRed,
	}.ConvertAll(c => c.ToV2().parentType).ToHashSet().ToList();

	public static List<TrainCarType_v2> perishableCars = new List<TrainCarType>
	{
		FlatbedEmpty,
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		RefrigeratorWhite,
	}.ConvertAll(c => c.ToV2().parentType).ToHashSet().ToList();

	public static List<TrainCarType_v2> bulkCars = new List<TrainCarType>
	{
		HopperBrown, HopperTeal, HopperYellow,
		GondolaGray, GondolaGreen, GondolaRed,
	}.ConvertAll(c => c.ToV2().parentType).ToHashSet().ToList();

	public static List<TrainCarType> tankers = new List<TrainCarType>{
		TankWhite, TankYellow, TankChrome,
		TankBlue, TankOrange,
		TankBlack,
	};
}