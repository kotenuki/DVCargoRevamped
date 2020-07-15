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
		private static bool GameReady = false;
		static void Load(UnityModManager.ModEntry modEntry)
		{
			modEntry.Logger.Log("DVCargoMod is loaded");
			//var harmony = new Harmony(modEntry.Info.Id);
			//harmony.PatchAll(Assembly.GetExecutingAssembly());
			modifyCars(modEntry);
		}

		private static void modifyCars(UnityModManager.ModEntry modEntry)
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

			modEntry.Logger.Log("Getting dictionary...");
			// Thank you to Cadde in the DV Discord
			var memberInfo = typeof(CargoTypes)
				.GetField("_containerTypeToCarTypes", BindingFlags.NonPublic | BindingFlags.Static);
			if (memberInfo != null)
			{
				var dic = (Dictionary<CargoContainerType, List<TrainCarType>>)memberInfo.GetValue(null);
				modEntry.Logger.Log("Dictionary loaded");
				foreach (var key in dic.Keys)
				{
					var cargoContainerType = key;
					var listOfTrainCarTypes = dic[key];
					modEntry.Logger.Log(String.Format("Found key {0}", key));
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
					switch (cargoContainerType)
					{
						case CargoContainerType.TankerOil:
						{
							listOfTrainCarTypes.AddRange(gasCars);
							listOfTrainCarTypes.AddRange(chemCars);
							break;
						}
						case CargoContainerType.TankerGas:
						{
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(chemCars);
							break;
						}
						case CargoContainerType.TankerChem:
						{
							listOfTrainCarTypes.AddRange(oilCars);
							listOfTrainCarTypes.AddRange(gasCars);
							break;
						}
						default:
						{
							break;
						}
					}
				}
				modEntry.Logger.Log("Dictionary modified");
			}
		}
	}
}