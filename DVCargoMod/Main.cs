using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;
using System.Reflection;
using DV.Logic.Job;

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
				Debug.Log("Dictionary modified");
			}
		}
	}
}