using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;
using System.Reflection;
using DV.Logic.Job;

namespace DVCargoMod
{
	class Main
	{
		static void Load(UnityModManager.ModEntry modEntry)
		{
			var harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(CargoTypes), "ContainerTypeToCarTypes")]
	class CargoTypes_Patch
	{
		static bool Prefix(ref Dictionary<CargoContainerType, List<TrainCarType>> __result)
		{
			var oilCars  = new List<TrainCarType>() { TrainCarType.TankChrome, TrainCarType.TankWhite, TrainCarType.TankYellow };
			var gasCars  = new List<TrainCarType>() { TrainCarType.TankBlue, TrainCarType.TankOrange };
			var chemCars = new List<TrainCarType>() { TrainCarType.TankBlack };

			foreach (KeyValuePair<CargoContainerType, List<TrainCarType>> kvpair in __result)
			{
				var cargoContainerType = kvpair.Key;
				var listOfTrainCarTypes = kvpair.Value;
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
						__result[cargoContainerType].AddRange(gasCars);
						__result[cargoContainerType].AddRange(chemCars);
						break;
					}
					case CargoContainerType.TankerGas:
					{
						__result[cargoContainerType].AddRange(oilCars);
						__result[cargoContainerType].AddRange(chemCars);
						break;
					}
					case CargoContainerType.TankerChem:
					{
						__result[cargoContainerType].AddRange(oilCars);
						__result[cargoContainerType].AddRange(gasCars);
						break;
					}
				}

			}

			return true;
		}
	}

}
