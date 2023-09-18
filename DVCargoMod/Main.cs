using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

using static DV.ThingTypes.CargoType;
using static DV.ThingTypes.TrainCarType;

namespace DvCargoMod;

public static class Main
{
	public static UnityModManager.ModEntry? mod;
	public const string PREFIX = "[DvCargoMod] ";

	private const bool SKIP_ORIGINAL = false;
	private const bool KEEP_ORIGINAL = true;
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

// [HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.RecalculateCaches))]
// class DVObjectModel_RecalculateCaches_Patch
// {
// 	static void Postfix()
// 	{
// 		Main.Unify();
// 	}
// }
[HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.CarTypeToLoadableCargo), MethodType.Getter)]
class DVObjectModel_CarTypeToLoadableCargo_Patch
{
	static void Postfix(ref Dictionary<TrainCarType_v2, List<CargoType_v2>> __result)
	{
		// Debug.Log(Main.PREFIX + "DVObjectModel.CarTypeToLoadableCargo called");
		var carToCargo = __result.ToDictionary(e => e.Key, e => e.Value);
		foreach (KeyValuePair<TrainCarType_v2, List<CargoType_v2>> pair in carToCargo)
		{
			// Debug.Log(Main.PREFIX + pair.Key.id.ToString());
			if (pair.Key.id.Contains("Tank"))
			{
				// Debug.Log(Main.PREFIX + $"adding tank car cargoes to car {pair.Key.id}");
				__result[pair.Key] = Cargoes.tankCarCargoes;
			}
			// else
			// {
			// 	carToCargo.AddItem(pair);
			// }
		}
		// foreach (KeyValuePair<TrainCarType_v2, List<CargoType_v2>> pair in __result)
		// {
		// 	Debug.Log(Main.PREFIX + $"{pair.Key}={string.Join(",", pair.Value.ConvertAll(e => e.v1.ToString()))}");
		// }
	}
}
[HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.CargoToLoadableCarTypes), MethodType.Getter)]
class DVObjectModel_CargoToLoadableCarTypes_Patch
{
	static void Postfix(ref Dictionary<CargoType_v2, List<TrainCarType_v2>> __result)
	{
		// Debug.Log(Main.PREFIX + "DVObjectModel.CargoToLoadableCarTypes called");
		var cargoToCar = __result.ToDictionary(e => e.Key, e => e.Value);
		foreach (KeyValuePair<CargoType_v2, List<TrainCarType_v2>> pair in cargoToCar)
		{
			// Debug.Log(Main.PREFIX + pair.Key.id.ToString());
			var cargo = pair.Key;
			if (Cargoes.tankCarCargoes.Contains(cargo))
			{
				// Debug.Log(Main.PREFIX + $"adding fluid cars to cargo {pair.Key.id}");
				// Debug.Log(Main.PREFIX + $"{__result[pair.Key].ConvertAll(e => e.v1)}");
				var loadableCars = new List<CargoType_v2.LoadableInfo>();
				foreach (var car in Cars.fluidCars)
				{
					var prefabs = new GameObject[] { };
					foreach (var livery in car.liveries)
					{
						if (TrainCarAndCargoDamageProperties.IsCargoFlammable(cargo.v1) && Cars.tankers.Contains(livery.v1))
						{
							prefabs = LoadableInfos.tankFlammable.cargoPrefabVariants;
						}
						else if (TrainCarAndCargoDamageProperties.IsCargoExplosive(cargo.v1) && Cars.tankers.Contains(livery.v1))
						{
							prefabs = LoadableInfos.tankExplosive.cargoPrefabVariants;
						}
						else if (TrainCarAndCargoDamageProperties.IsCargoCorrosiveLiquid(cargo.v1) && Cars.tankers.Contains(livery.v1))
						{
							prefabs = LoadableInfos.tankCorrosive.cargoPrefabVariants;
						}
					}
					var info = new CargoType_v2.LoadableInfo(car, prefabs);
					loadableCars.Add(info);
				}
				cargo.loadableCarTypes = loadableCars.Distinct().ToArray();
				__result[cargo] = Cars.fluidCars;

				foreach (var e in pair.Key.loadableCarTypes)
				{
					Debug.Log(Main.PREFIX + $"loadableCarTypes {pair.Key.v1} {e.carType} {string.Join(",", e.cargoPrefabVariants.ToList().ConvertAll(p => p.name))}");
				}
			}
			// else
			// {
			// 	cargoToCar.AddItem(pair);
			// }
		}
		// foreach (KeyValuePair<CargoType_v2, List<TrainCarType_v2>> pair in __result)
		// {
		// 	Debug.Log(Main.PREFIX + $"{pair.Key}={string.Join(",", pair.Value.ConvertAll(e => e.id))}");
		// }
	}
}

static class Cargoes
{
	public static List<CargoType_v2> tankCarCargoes = new List<CargoType>
	{
		CrudeOil, Diesel, Gasoline,
		Methane, Alcohol,
		Ammonia, SodiumHydroxide,
		Argon, Nitrogen, CryoHydrogen, CryoOxygen,
		ChemicalsIskar, ChemicalsSperex,
	}.ConvertAll(c => c.ToV2());

	public static List<CargoType_v2> nonPerishableCargoes = new List<CargoType>
	{
		ElectronicsAAG, ElectronicsIskar, ElectronicsKrugmann, ElectronicsNovae, ElectronicsTraeg, ClothingNeoGamma,
		ClothingNovae, ClothingObco, ClothingTraeg,
		ChemicalsIskar, ChemicalsSperex,
		Boards, Plywood,
		SteelBentPlates, SteelBillets, SteelRails, SteelRolls, SteelSlabs,
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
	}.ConvertAll(c => c.ToV2());


	public static List<CargoType_v2> perishableCargoes = new List<CargoType>
	{
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
		Chickens, Cows, Goats, Pigs, Sheep,
	}.ConvertAll(c => c.ToV2());

	public static List<CargoType_v2> bulkCargoes = new List<CargoType>
	{
		CargoType.Coal, IronOre,
		Logs,
		ScrapMetal
	}.ConvertAll(c => c.ToV2());


}
static class LoadableInfos
{
	public static CargoType_v2.LoadableInfo tankFlammable = CargoType.CrudeOil.ToV2().loadableCarTypes[0];
	public static CargoType_v2.LoadableInfo tankExplosive = CargoType.Gasoline.ToV2().loadableCarTypes[0];
	public static CargoType_v2.LoadableInfo tankCorrosive = CargoType.Ammonia.ToV2().loadableCarTypes[0];

	public static CargoType_v2.LoadableInfo flatcarAsph = CargoType.Argon.ToV2().loadableCarTypes[0];
	public static CargoType_v2.LoadableInfo flatcarIskar = CargoType.ChemicalsIskar.ToV2().loadableCarTypes[0];
	public static CargoType_v2.LoadableInfo flatcarSperex = CargoType.ChemicalsSperex.ToV2().loadableCarTypes[0];
	public static CargoType_v2.LoadableInfo flatcarExplosive = CargoType.CryoHydrogen.ToV2().loadableCarTypes[0];
	public static CargoType_v2.LoadableInfo flatcarOxy = CargoType.CryoOxygen.ToV2().loadableCarTypes[0];
}

static class Cars
{
	public static List<TrainCarType_v2> fluidCars = new List<TrainCarType>
	{
		// FlatbedEmpty,
		TankWhite, TankYellow, TankChrome,
		TankBlue, TankOrange,
		TankBlack,
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
