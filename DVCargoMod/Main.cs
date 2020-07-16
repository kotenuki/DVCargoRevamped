using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityModManagerNet;

using HarmonyLib;
using DV.Logic.Job;

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

			// Add cargo to cars:
			/* TankerOil	--> Ammonia, SodiumHydroxide, Alcohol, Methane
			 * TankerGas	--> Ammonia, SodiumHydroxide, CrudeOil, Diesel, Gasoline
			 * TankerChem	--> Alcohol, Methane, CrudeOil, Diesel, Gasoline
			 */
			// So, need to access the train car list to add the car type
			// Add the TrainCarTypes to the CargoContainerType's List:
			/* TankerOil	--> TankOrange, TankBlue, TankBlack
			 * TankerGas	--> TankYellow, TankWhite, TankChrome, TankBlack
			 * TankerChem	--> TankORange, TankBlue, TankYellow, TankWhite, TankChrome
			 */

			//Debug.Log("Getting dictionary...");
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
							Debug.Log("Added!");
							break;
						}
						case CargoContainerType.TankerGas:
						{
							Debug.Log("Adding to " + key + "...");
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(chemCars);
							Debug.Log("Added!");
							break;
						}
						case CargoContainerType.TankerChem:
						{
							Debug.Log("Adding to " + key + "...");
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(gasCars);
							Debug.Log("Added!");
							break;
						}
					}
				}

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

				Debug.Log("Dictionary modified");

				var prop = typeof(CargoTypes).GetProperty("cargoTypeToSupportedCarContainer",
					BindingFlags.Static);
				if (prop != null)
				{
					Debug.Log("Modifying cargoTypeToSupportedCarContainer...");
					var dict = (Dictionary<CargoType, List<CargoContainerType>>) prop.GetValue(typeof(CargoTypes));

					var keys = dict.Keys;
					foreach (var key in keys)
					{
						Debug.Log("Found key " + key);
						var cargoType = key;
						var listOfCargoContainerTypes = dict[key];
						// cargoTypes to add:
						/* CargoType.CrudeOil, Diesel, Gasoline	--> CargoContainerType.TankerGas, CargoContainerType.TankerChem
						 * CargoType.Methane, Alcohol			--> CargoContainerType.TankerOil, CargoContainerType.TankerChem
						 * CargoType.Ammonia, SodiumHydroxide	--> CargoContainerType.TankerOil, CargoContainerType.TankerGas
						 */
						switch (cargoType)
						{
							case CargoType.CrudeOil:
							case CargoType.Diesel:
							case CargoType.Gasoline:
							{
								Debug.Log("Adding to " + key + "...");
								listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
								listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
								Debug.Log("Added!");
								break;
							}
							case CargoType.Methane:
							case CargoType.Alcohol:
							{
								Debug.Log("Adding to " + key + "...");
								listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
								listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
								Debug.Log("Added!");
								break;
							}
							case CargoType.Ammonia:
							case CargoType.SodiumHydroxide:
							{
								Debug.Log("Adding to " + key + "...");
								listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
								listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
								Debug.Log("Added!");
								break;
							}
						}

					}

					Debug.Log("cargoTypeToSupportedCarContainer modified!");

					Debug.Log("Contents of cargoTypeToSupportedCarContainer:");
					foreach (var key in dict.Keys)
					{
						string o = "{";
						foreach (var ele in dict[key])
						{
							o += ele + ", ";
						}

						o += "}";
						Debug.Log(key + " --> " + o);
					}
				}
				else
				{
					Debug.Log("cargoTypeToSupportedCarContainer was null");
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