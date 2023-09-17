using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

using DV.Logic.Job;
using System.Collections.Generic;

namespace DvCargoMod;

public static class Main
{
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		Harmony? harmony = null;

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
						listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
						break;
					}
				case CargoContainerType.TankerGas:
					{
						listOfTrainCarTypes.AddRange(oilCars);
						listOfTrainCarTypes.AddRange(chemCars);
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
						break;
					}
				case CargoContainerType.TankerChem:
					{
						listOfTrainCarTypes.AddRange(oilCars);
						listOfTrainCarTypes.AddRange(gasCars);
						listOfTrainCarTypes.Add(TrainCarType.FlatbedEmpty);
						listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
						break;
					}
				case CargoContainerType.Flatcar:
					{
						listOfTrainCarTypes.AddRange(oilCars);
						listOfTrainCarTypes.AddRange(gasCars);
						listOfTrainCarTypes.AddRange(chemCars);
						listOfTrainCarTypes = listOfTrainCarTypes.Distinct().ToList();
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
						listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
						break;
					}
				case CargoType.Methane:
				case CargoType.Alcohol:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerChem);
						listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
						listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
						break;
					}
				case CargoType.Ammonia:
				case CargoType.SodiumHydroxide:
					{
						listOfCargoContainerTypes.Add(CargoContainerType.TankerOil);
						listOfCargoContainerTypes.Add(CargoContainerType.TankerGas);
						listOfCargoContainerTypes.Add(CargoContainerType.Flatcar);
						listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
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
						listOfCargoContainerTypes = listOfCargoContainerTypes.Distinct().ToList();
						break;
					}
			}
		}

		Debug.Log("cargoTypeToSupportedCarContainer modified!");

		//TODO: cleanup dupe entries in lists
		//HashSets?
		var d = new Dictionary<CargoContainerType, List<TrainCarType>>();
		foreach (var key in __result.Keys)
		{
			d[key] = new HashSet<TrainCarType>(__result[key]).ToList();
		}
		__result = d;

		var c = new Dictionary<CargoType, List<CargoContainerType>>();
		foreach (var key in cargoTypeToSupportedCarContainer.Keys)
		{
			c[key] = new HashSet<CargoContainerType>(cargoTypeToSupportedCarContainer[key]).ToList();
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

		var CargoContainerToAvailableCargoTypeModels =
			(Dictionary<CargoContainerType, Dictionary<CargoType, List<string>>>)typeof(CargoModelsData).GetField("CargoContainerToAvailableCargoTypeModels",
				BindingFlags.Public | BindingFlags.Static).GetValue(
				typeof(CargoModelsData));

		var cargoTypeToSpriteIcon = (Dictionary<CargoType, Sprite>)typeof(IconsSpriteMap).GetField("cargoTypeToSpriteIcon",
			BindingFlags.Public | BindingFlags.Static).GetValue(typeof(IconsSpriteMap));

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

[HarmonyPatch(typeof(StaticShuntingLoadJobDefinition), "GenerateJob")]
class StaticShuntingLoadJobDefinition_GenerateJob_Patch
{
	static bool Prefix(
		StaticShuntingLoadJobDefinition __instance, ref DV.Logic.Job.Job ___job,
		Station jobOriginStation,
		float jobTimeLimit = 0.0f,
		float initialWage = 0.0f,
		string forcedJobId = null,
		JobLicenses requiredLicenses = JobLicenses.Basic)
	{
		try
		{
			if (__instance.carsPerStartingTrack != null && __instance.carsPerStartingTrack.Count > 0 &&
				(__instance.loadMachine != null && __instance.loadData != null) &&
				(__instance.loadData.Count > 0 && __instance.destinationTrack != null))
			{
				___job = JobsGenerator.CreateShuntingLoadJob(jobOriginStation, __instance.chainData,
					__instance.carsPerStartingTrack, __instance.destinationTrack, __instance.loadMachine,
					__instance.loadData, __instance.forceCorrectCargoStateOnCars, jobTimeLimit, initialWage,
					forcedJobId, requiredLicenses);
			}
			else
			{
				__instance.carsPerStartingTrack = (List<CarsPerTrack>)null;
				__instance.loadMachine = (WarehouseMachine)null;
				__instance.loadData = (List<CarsPerCargoType>)null;
				__instance.destinationTrack = (Track)null;
				___job = (DV.Logic.Job.Job)null;
				Debug.LogError((object)"ShuntingLoad job not created, bad parameters",
					(UnityEngine.Object)__instance);
			}
		}
		catch (Exception e)
		{
			Debug.Log(e);
			string o = "cargoTypes: {";
			foreach (var cpct in __instance.loadData)
			{
				o += cpct.cargoType + ", ";
			}
			o += "}\n";
			Debug.Log(o);
		}
		return false;
	}
}

[HarmonyPatch(typeof(StaticShuntingUnloadJobDefinition), "GenerateJob")]
class StaticShuntingUnloadJobDefinition_GenerateJob_Patch
{
	static bool Prefix(
		StaticShuntingUnloadJobDefinition __instance, ref DV.Logic.Job.Job ___job,
		Station jobOriginStation,
		float jobTimeLimit = 0.0f,
		float initialWage = 0.0f,
		string forcedJobId = null,
		JobLicenses requiredLicenses = JobLicenses.Basic
	)
	{
		try
		{
			if (__instance.startingTrack != null && __instance.unloadMachine != null && (__instance.unloadData != null && __instance.unloadData.Count > 0) && (__instance.carsPerDestinationTrack != null && __instance.carsPerDestinationTrack.Count > 0))
			{
				___job = JobsGenerator.CreateShuntingUnloadJob(jobOriginStation, __instance.chainData, __instance.startingTrack, __instance.carsPerDestinationTrack, __instance.unloadMachine, __instance.unloadData, __instance.forceCorrectCargoStateOnCars, jobTimeLimit, initialWage, forcedJobId, requiredLicenses);
			}
			else
			{
				___job = (DV.Logic.Job.Job)null;
				Debug.LogError((object)"ShuntingUnload job not created, bad parameters", (UnityEngine.Object)__instance);
			}
		}
		catch (Exception e)
		{
			Debug.Log(e);
			string o = "cargoTypes: {";
			foreach (var cpct in __instance.unloadData)
			{
				o += cpct.cargoType + ", ";
			}
			o += "}\n";
			Debug.Log(o);
		}
		return false;
	}
}

[HarmonyPatch(typeof(StaticTransportJobDefinition), "GenerateJob")]
class StaticTransportJobDefinition_GenerateJob_Patch
{
	static bool Prefix(
		StaticTransportJobDefinition __instance, ref DV.Logic.Job.Job ___job,
		Station jobOriginStation,
		float jobTimeLimit = 0.0f,
		float initialWage = 0.0f,
		string forcedJobId = null,
		JobLicenses requiredLicenses = JobLicenses.Basic
		)
	{
		try
		{
			if (__instance.trainCarsToTransport != null && __instance.trainCarsToTransport.Count > 0 && (__instance.transportedCargoPerCar.Count == __instance.trainCarsToTransport.Count && __instance.cargoAmountPerCar.Count == __instance.trainCarsToTransport.Count) && (__instance.startingTrack != null && __instance.destinationTrack != null))
			{
				___job = JobsGenerator.CreateTransportJob(jobOriginStation, __instance.chainData, __instance.trainCarsToTransport, __instance.destinationTrack, __instance.startingTrack, __instance.transportedCargoPerCar, __instance.cargoAmountPerCar, __instance.forceCorrectCargoStateOnCars, jobTimeLimit, initialWage, forcedJobId, requiredLicenses);
			}
			else
			{
				__instance.trainCarsToTransport = (List<Car>)null;
				__instance.startingTrack = (Track)null;
				__instance.destinationTrack = (Track)null;
				__instance.transportedCargoPerCar = (List<CargoType>)null;
				__instance.cargoAmountPerCar = (List<float>)null;
				___job = (DV.Logic.Job.Job)null;
				Debug.LogError((object)"Transport job not created, bad parameters!");
			}
		}
		catch (Exception e)
		{
			Debug.Log(e);
			string o = "cargoTypes: {";
			foreach (var ct in __instance.transportedCargoPerCar)
			{
				o += ct + ", ";
			}
			o += "}\n";
			Debug.Log(o);
		}
		return false;
	}
}
