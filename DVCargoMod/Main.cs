using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityModManagerNet;

using HarmonyLib;
using DV.Logic.Job;
using DV.RenderTextureSystem.BookletRender;

namespace DVCargoMod
{
	static class Main
	{
		static void Load(UnityModManager.ModEntry modEntry) {
			Debug.Log("DVCargoMod is loaded");
			var harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(CargoTypes))]
	[HarmonyPatch("ContainerTypeToCarTypes", MethodType.Getter)]
	class ContainerTypeToCarTypes_Getter_Patch
	{
		static void Postfix(ref Dictionary<CargoContainerType, List<TrainCarType>> __result)
		{
			var oilCars = new List<TrainCarType>()
				{TrainCarType.TankChrome, TrainCarType.TankWhite, TrainCarType.TankYellow};
			var gasCars = new List<TrainCarType>() { TrainCarType.TankBlue, TrainCarType.TankOrange };
			var chemCars = new List<TrainCarType>() { TrainCarType.TankBlack };
			var boxCars = new List<TrainCarType>()
				{TrainCarType.BoxcarBrown, TrainCarType.BoxcarGreen, TrainCarType.BoxcarPink, TrainCarType.BoxcarRed};

			//// Thank you to Cadde in the DV Discord
			//var memberInfo = typeof(CargoTypes)
			//	.GetField("_containerTypeToCarTypes", BindingFlags.NonPublic | BindingFlags.Static);
			//var dic = (Dictionary<CargoContainerType, List<TrainCarType>>)memberInfo.GetValue(null);
			Debug.Log("Dictionary loaded");

			Debug.Log("Modifying _containerTypeToCarTypes...");
			foreach (var key in __result.Keys)
			{
				var cargoContainerType = key;
				var listOfTrainCarTypes = __result[key];
				switch (cargoContainerType)
				{
					case CargoContainerType.TankerOil:
					{
						listOfTrainCarTypes.AddRange(gasCars);
						listOfTrainCarTypes.AddRange(chemCars);
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						break;
					}
					case CargoContainerType.TankerGas:
					{
						listOfTrainCarTypes.AddRange(oilCars);
						listOfTrainCarTypes.AddRange(chemCars);
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						break;
					}
					case CargoContainerType.TankerChem:
					{
						listOfTrainCarTypes.AddRange(oilCars);
						listOfTrainCarTypes.AddRange(gasCars);
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						break;
					}
					case CargoContainerType.Flatcar:
					{
						//listOfTrainCarTypes.AddRange(oilCars);
						//listOfTrainCarTypes.AddRange(gasCars);
						//listOfTrainCarTypes.AddRange(chemCars);
						listOfTrainCarTypes.AddRange(boxCars);
						listOfTrainCarTypes.Add(TrainCarType.RefrigeratorWhite);
						break;
					}
					case CargoContainerType.Refrigerator:
					{
						listOfTrainCarTypes.AddRange(boxCars);
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						break;
					}
					case CargoContainerType.Boxcar:
					{
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						listOfTrainCarTypes.Add(TrainCarType.RefrigeratorWhite);
						break;
					}
				}
			}
			Debug.Log("Dictionary modified");

			var cargoTypeToSupportedCarContainer = (Dictionary<CargoType, List<CargoContainerType>>) typeof(CargoTypes).GetField("cargoTypeToSupportedCarContainer",
				BindingFlags.NonPublic | BindingFlags.Static).GetValue(typeof(CargoTypes));

			Debug.Log("Modifying cargoTypeToSupportedCarContainer...");

			var keys = cargoTypeToSupportedCarContainer.Keys;
			foreach (var key in keys)
			{
				var cargoType = key;
				var listOfCargoContainerTypes = cargoTypeToSupportedCarContainer[key];

				switch (cargoType)
				{
					case CargoType.CrudeOil:
					case CargoType.Diesel:
					case CargoType.Gasoline:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
						listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
						break;
					}
					case CargoType.Methane:
					case CargoType.Alcohol:
					{
						
						listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
						listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
						break;
					}
					case CargoType.Ammonia:
					case CargoType.SodiumHydroxide:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
						listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
						break;
					}
					case CargoType.Argon:
					case CargoType.Nitrogen:
					case CargoType.CryoHydrogen:
					case CargoType.CryoOxygen:
					case CargoType.ChemicalsIskar:
					case CargoType.ChemicalsSperex:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
						break;
					}
					case CargoType.ClothingNeoGamma:
					case CargoType.ClothingNovae:
					case CargoType.ClothingObco:
					case CargoType.ClothingTraeg:
					case CargoType.ElectronicsAAG:
					case CargoType.ElectronicsIskar:
					case CargoType.ElectronicsKrugmann:
					case CargoType.ElectronicsNovae:
					case CargoType.ElectronicsTraeg:
					case CargoType.Boards:
					case CargoType.Plywood:
					case CargoType.SteelBentPlates:
					case CargoType.SteelBillets:
					case CargoType.SteelRolls:
					case CargoType.SteelSlabs:
					case CargoType.SteelRails:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.Boxcar);
						break;
					}
					case CargoType.Bread:
					case CargoType.Chickens:
					case CargoType.Cows:
					case CargoType.Goats:
					case CargoType.Pigs:
					case CargoType.Sheep:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
						listOfCargoContainerTypes.Add(CargoContainerType.Refrigerator);
						break;
					}
					case CargoType.CannedFood:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.Refrigerator);
						goto case CargoType.CatFood;
					}
					case CargoType.CatFood:
					case CargoType.DairyProducts:
					case CargoType.Medicine:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.Boxcar);
						break;
					}
				}
				Debug.Log("Added!");
			}

			Debug.Log("cargoTypeToSupportedCarContainer modified!");

			var CargoContainerToAvailableCargoTypeModels =
				(Dictionary<CargoContainerType, Dictionary<CargoType, List<string>>>) typeof(CargoModelsData).GetField("CargoContainerToAvailableCargoTypeModels",
					BindingFlags.Public | BindingFlags.Static).GetValue(
					typeof(CargoModelsData));

			var cargoTypeToSpriteIcon = (Dictionary<CargoType, Sprite>) typeof(IconsSpriteMap).GetField("cargoTypeToSpriteIcon",
				BindingFlags.Public | BindingFlags.Static).GetValue(typeof(IconsSpriteMap));

			var flatcarFluidCargoTypes = new List<CargoType>()
				{CargoType.CrudeOil, CargoType.Gasoline, CargoType.Diesel, CargoType.Methane, CargoType.Alcohol, CargoType.Ammonia, CargoType.SodiumHydroxide};

			var flatcarWhiteContainerCargoTypes = new List<CargoType>() { CargoType.Bread, CargoType.Chickens, CargoType.Cows, CargoType.Goats, CargoType.Pigs, CargoType.Sheep };

			foreach (var cargoType in flatcarFluidCargoTypes)
			{
				// Add cargo models to flatcars
				CargoContainerToAvailableCargoTypeModels[CargoContainerType.Flatcar][cargoType] = new List<string>() { "C_FlatcarISOTankYellow2_Explosive" };
				// Add icons to flatcars in job booklet	
				cargoTypeToSpriteIcon[cargoType] = Resources.Load("CarFlatcar_TankISO", typeof(Sprite)) as Sprite;
			}

			foreach (var cargoType in flatcarWhiteContainerCargoTypes)
			{
				CargoContainerToAvailableCargoTypeModels[CargoContainerType.Flatcar][cargoType] = new List<string>() { "C_FlatcarContainerWhite" };
				cargoTypeToSpriteIcon[cargoType] = Resources.Load("CarFlatcar_ContainerSunOmni", typeof(Sprite)) as Sprite;
			}

			var r = new Dictionary<CargoContainerType, List<TrainCarType>>();
			foreach (var k in __result.Keys)
			{
				r[k] = new List<TrainCarType>();
				r[k].AddRange(__result[k].Distinct().ToList());
			}
			__result = r;

			var c = new Dictionary<CargoType, List<CargoContainerType>>();
			foreach (var k in cargoTypeToSupportedCarContainer.Keys)
			{
				c[k] = new List<CargoContainerType>();
				c[k].AddRange(cargoTypeToSupportedCarContainer[k].Distinct().ToList());
			}
			cargoTypeToSupportedCarContainer = c;

			  Debug.Log("Contents of _containerTypeToCarTypes:");
			foreach (var key in __result.Keys)
			{
				string o = "{";
				foreach (var ele in __result[key])
				{
					o += ele + ", ";
				}

				o += "}";
				Debug.Log(key + " --> " + o);
			}

			Debug.Log("Contents of cargoTypeToSupportedCarContainer:");
			foreach (var key in cargoTypeToSupportedCarContainer.Keys)
			{
				string o = "{";
				foreach (var ele in cargoTypeToSupportedCarContainer[key])
				{
					o += ele + ", ";
				}

				o += "}";
				Debug.Log(key + " --> " + o);
			}
		}
	}
	// "Error while creating transport account" --> DV.Logic.Jobs.JobGenerator.CreateTransportJob()
	//class CanCarContainCargoType_Patch
	//{
	//	static void Prefix(ref bool __result)
	//	{

	//	}
	//}
}