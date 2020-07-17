using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityModManagerNet;

using HarmonyLib;
using DV.Logic.Job;
using DV.RenderTextureSystem.BookletRender;

namespace DVCargoMod
{
	[EnableReloading]
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

			//// Thank you to Cadde in the DV Discord
			//var memberInfo = typeof(CargoTypes)
			//	.GetField("_containerTypeToCarTypes", BindingFlags.NonPublic | BindingFlags.Static);
			if (__result != null)
			{
				//var dic = (Dictionary<CargoContainerType, List<TrainCarType>>)memberInfo.GetValue(null);
				Debug.Log("Dictionary loaded");
				Debug.Log("Modifying _containerTypeToCarTypes...");
				foreach (var key in __result.Keys)
				{
					var cargoContainerType = key;
					var listOfTrainCarTypes = __result[key];
					Debug.Log("Found key " + key);

					switch (cargoContainerType)
					{
						case CargoContainerType.TankerOil:
						{
							Debug.Log("Adding to " + key + "...");
							listOfTrainCarTypes.AddRange(gasCars);
							listOfTrainCarTypes.AddRange(chemCars);
							listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
							listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
						case CargoContainerType.TankerGas:
						{
							Debug.Log("Adding to " + key + "...");
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(chemCars);
							listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
							listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
						case CargoContainerType.TankerChem:
						{
							Debug.Log("Adding to " + key + "...");
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(gasCars);
							listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
							listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
						case CargoContainerType.Flatcar:
						{
							Debug.Log("Adding to " + key + "...");
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(gasCars);
							listOfTrainCarTypes.AddRange(chemCars);
							listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
					}
				}

				Debug.Log("Dictionary modified");

				var prop = typeof(CargoTypes).GetField("cargoTypeToSupportedCarContainer",
					BindingFlags.NonPublic | BindingFlags.Static);
				var cargoTypeToSupportedCarContainer = (Dictionary<CargoType, List<CargoContainerType>>)prop.GetValue(typeof(CargoTypes));
				
				Debug.Log("Modifying cargoTypeToSupportedCarContainer...");

				var keys = cargoTypeToSupportedCarContainer.Keys;
				foreach (var key in keys)
				{
					Debug.Log("Found key " + key);
					var cargoType = key;
					var listOfCargoContainerTypes = cargoTypeToSupportedCarContainer[key];

					switch (cargoType)
					{
						case CargoType.CrudeOil:
						case CargoType.Diesel:
						case CargoType.Gasoline:
						{
							Debug.Log("Adding to " + key + "...");
							listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
							listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
							listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
							listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
						case CargoType.Methane:
						case CargoType.Alcohol:
						{
							Debug.Log("Adding to " + key + "...");
							listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
							listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
							listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
							listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
						case CargoType.Ammonia:
						case CargoType.SodiumHydroxide:
						{
							Debug.Log("Adding to " + key + "...");
							listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
							listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
							listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
							listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
						case CargoType.Argon:
						case CargoType.Nitrogen:
						case CargoType.CryoHydrogen:
						case CargoType.CryoOxygen:
						case CargoType.ChemicalsIskar:
						case CargoType.ChemicalsSperex:
						{
							Debug.Log("Adding to " + key + "...");
							listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
							listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
							listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
							listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
							Debug.Log("Added!");
							break;
						}
					}
				}

				Debug.Log("cargoTypeToSupportedCarContainer modified!");

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

				
				var cctactm = typeof(CargoModelsData).GetField("CargoContainerToAvailableCargoTypeModels",
					BindingFlags.Public | BindingFlags.Static);
				var CargoContainerToAvailableCargoTypeModels =
					(Dictionary<CargoContainerType, Dictionary<CargoType, List<string>>>) cctactm.GetValue(
						typeof(CargoModelsData));

				var cttsi = typeof(IconsSpriteMap).GetField("cargoTypeToSpriteIcon",
					BindingFlags.Public | BindingFlags.Static);
				var cargoTypeToSpriteIcon = (Dictionary<CargoType, Sprite>)cttsi.GetValue(typeof(IconsSpriteMap));

				var flatcarFluidCargoTypes = new List<CargoType>()
					{CargoType.CrudeOil, CargoType.Gasoline, CargoType.Diesel, CargoType.Methane, CargoType.Alcohol, CargoType.Ammonia, CargoType.SodiumHydroxide};

				foreach (var cargoType in flatcarFluidCargoTypes)
				{
					// Add cargo models to flatcars
					CargoContainerToAvailableCargoTypeModels[CargoContainerType.Flatcar][cargoType] = new List<string>() { "C_FlatcarISOTankYellow2_Explosive" };
					// Add icons to flatcars in job booklet	
					cargoTypeToSpriteIcon[cargoType] = Resources.Load("CarFlatcar_TankISO", typeof(Sprite)) as Sprite;
				}

				


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