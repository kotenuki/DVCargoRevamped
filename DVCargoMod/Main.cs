using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using System.Collections.Generic;
using DV.ThingTypes;

using UnityEngine;
using System.Linq;
using DV.Logic.Job;

using static DV.ThingTypes.CargoType;
using static DV.ThingTypes.TrainCarType;
using DV.ThingTypes.TransitionHelpers;

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

			// Traverse.Create(DV.Globals.G).Field<DVObjectModel>("types").Value
			// Debug.Log(PREFIX + JsonConvert.SerializeObject(DV.Globals.G.Types.CargoToLoadableCarTypes));
			DV.Globals.G.Types.RecalculateCaches();
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
	static void Postfix()
	{
		Debug.Log(Main.PREFIX + "Unifying cargoes...");

		var carToCargo = DV.Globals.G.Types.CarTypeToLoadableCargo.ToDictionary(e => e.Key, e => e.Value);

		foreach (KeyValuePair<TrainCarType_v2, List<CargoType_v2>> pair in carToCargo)
		{
			// Debug.Log(Main.PREFIX + pair.Key.id.ToString());
			if (pair.Key.id.Contains("Tank"))
			{
				// Debug.Log(Main.PREFIX + $"adding tank car cargoes to car {pair.Key.id}");
				DV.Globals.G.Types.CarTypeToLoadableCargo[pair.Key] = Cargoes.tankCarCargoes;
			}
		}

		var cargoToCar = DV.Globals.G.Types.CargoToLoadableCarTypes.ToDictionary(e => e.Key, e => e.Value);

		foreach (KeyValuePair<CargoType_v2, List<TrainCarType_v2>> pair in cargoToCar)
		{
			var cargo = pair.Key;
			// Debug.Log(Main.PREFIX + pair.Key.id.ToString());
			if (Cargoes.tankCarCargoes.Contains(cargo))
			{
				// Debug.Log(Main.PREFIX + $"adding fluid cars to cargo {pair.Key.id}");
				DV.Globals.G.Types.CargoToLoadableCarTypes[cargo] = Cars.fluidCars;
			}
		}
	}
}

static class Cargoes
{
	public static List<CargoType_v2> tankCarCargoes = new List<CargoType>()
	{
		CrudeOil, Diesel, Gasoline,
		Methane, Alcohol,
		Ammonia, SodiumHydroxide,
		Argon, Nitrogen, CryoHydrogen, CryoOxygen,
		ChemicalsIskar, ChemicalsSperex,
	}.ConvertAll(c => DV.Globals.G.Types.CargoType_to_v2[c]);

	public static List<CargoType_v2> nonPerishableCargoes = new List<CargoType>()
	{
		ElectronicsAAG, ElectronicsIskar, ElectronicsKrugmann, ElectronicsNovae, ElectronicsTraeg, ClothingNeoGamma,
		ClothingNovae, ClothingObco, ClothingTraeg,
		ChemicalsIskar, ChemicalsSperex,
		Boards, Plywood,
		SteelBentPlates, SteelBillets, SteelRails, SteelRolls, SteelSlabs,
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
	}.ConvertAll(c => DV.Globals.G.Types.CargoType_to_v2[c]);


	public static List<CargoType_v2> perishableCargoes = new List<CargoType>()
	{
		Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
		Medicine,
		Chickens, Cows, Goats, Pigs, Sheep,
	}.ConvertAll(c => DV.Globals.G.Types.CargoType_to_v2[c]);

	public static List<CargoType_v2> bulkCargoes = new List<CargoType>()
	{
		CargoType.Coal, IronOre,
		Logs,
		ScrapMetal
	}.ConvertAll(c => DV.Globals.G.Types.CargoType_to_v2[c]);
}

static class Cars
{
	public static List<TrainCarType_v2> fluidCars = new List<TrainCarType>()
	{
		FlatbedEmpty,
		TankWhite, TankYellow, TankChrome,
		TankBlue, TankOrange,
		TankBlack,
	}.ConvertAll(c => DV.Globals.G.Types.TrainCarType_to_v2[c].parentType);

	public static List<TrainCarType_v2> nonPerishableCars = new List<TrainCarType>()
	{
		FlatbedEmpty,
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		GondolaGray, GondolaGreen, GondolaRed,
	}.ConvertAll(c => DV.Globals.G.Types.TrainCarType_to_v2[c].parentType);

	public static List<TrainCarType_v2> perishableCars = new List<TrainCarType>()
	{
		FlatbedEmpty,
		BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
		RefrigeratorWhite,
	}.ConvertAll(c => DV.Globals.G.Types.TrainCarType_to_v2[c].parentType);

	public static List<TrainCarType_v2> bulkCars = new List<TrainCarType>()
	{
		HopperBrown, HopperTeal, HopperYellow,
		GondolaGray, GondolaGreen, GondolaRed,
	}.ConvertAll(c => DV.Globals.G.Types.TrainCarType_to_v2[c].parentType);
}
