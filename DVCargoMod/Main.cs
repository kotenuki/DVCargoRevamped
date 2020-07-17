using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityModManagerNet;

using HarmonyLib;
using DV.Logic.Job;
using DV.RenderTextureSystem.BookletRender;

namespace DVCargoMod
{
	static class Main
	{
		static void Load(UnityModManager.ModEntry modEntry)
		{
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
				(Dictionary<CargoContainerType, Dictionary<CargoType, List<string>>>) typeof(CargoModelsData).GetField("CargoContainerToAvailableCargoTypeModels",
					BindingFlags.Public | BindingFlags.Static).GetValue(
					typeof(CargoModelsData));

			var cargoTypeToSpriteIcon = (Dictionary<CargoType, Sprite>) typeof(IconsSpriteMap).GetField("cargoTypeToSpriteIcon",
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

	[HarmonyPatch(typeof(JobsGenerator))]
	[HarmonyPatch("CreateShuntingLoadJob")]
	class CreateShuntingLoadJob_Patch
	{
		static bool Prefix (
			out DV.Logic.Job.Job __result,
			Station jobOriginStation,
			StationsChainData chainData,
			Track startingTrack,
			List<CarsPerTrack> destinationTracksData,
			WarehouseMachine unloadMachine,
			List<CarsPerCargoType> carsUnloadData,
			bool forceFillCargoIfMissing = false,
			float timeLimit = 0.0f,
			float initialWage = 0.0f,
			string forcedJobId = null,
			JobLicenses requiredLicenses = JobLicenses.Basic)
		{
			if (destinationTracksData == null || destinationTracksData.Count == 0)
				throw new Exception(string.Format("Error while creating {0} job, {1} is null or empty!", JobType.ShuntingUnload, (object)nameof(destinationTracksData)));
			if (carsUnloadData == null || carsUnloadData.Count == 0)
				throw new Exception(string.Format("Error while creating {0} job, {1} is null or empty!", JobType.ShuntingUnload, (object)nameof(carsUnloadData)));
			List<CargoType> cargoTypePerCar = JobsGenerator.GetCargoTypePerCar(carsUnloadData);
			TransportTask transportTask1 = JobsGenerator.CreateTransportTask(carsUnloadData.SelectMany<CarsPerCargoType, Car>((Func<CarsPerCargoType, IEnumerable<Car>>)(loadData => (IEnumerable<Car>)loadData.cars)).ToList<Car>(), unloadMachine.WarehouseTrack, startingTrack, cargoTypePerCar);
			for (int i = 0; i < carsUnloadData.Count; i++)
			{
				// Changed
				if (carsUnloadData[i].cars.Any<Car>((Func<Car, bool>)(car => !CargoTypes.CanCarContainCargoType(car.carType, carsUnloadData[i].cargoType))))
					throw new Exception(string.Format("Error while creating {0} job, not all cars from {1}[{2}] {4} can carry {3}!", (object)JobType.ShuntingUnload, (object)nameof(carsUnloadData), (object)i, (object)carsUnloadData[i].cargoType, carsUnloadData[i]));
				
				if ((double)carsUnloadData[i].cars.Select<Car, float>((Func<Car, float>)(car => car.capacity)).Sum() < (double)carsUnloadData[i].totalCargoAmount)
					throw new Exception(string.Format("Error while creating {0} job, {1} {2} to unload is beyond {3}[{4}].cars capacity!", (object)JobType.ShuntingUnload, (object)carsUnloadData[i].totalCargoAmount, (object)carsUnloadData[i].cargoType, (object)nameof(carsUnloadData), (object)i));
				
				if (!unloadMachine.IsCargoSupported(carsUnloadData[i].cargoType))
					throw new Exception(string.Format("Error while creating {0} job, cargo type we want to unload [{1}] is not supported by {2}", (object)JobType.ShuntingUnload, (object)carsUnloadData[i].cargoType, (object)nameof(unloadMachine)));
				
				if ((double)carsUnloadData[i].cars.Select<Car, float>((Func<Car, float>)(car => car.LoadedCargoAmount)).Sum() < (double)carsUnloadData[i].totalCargoAmount || carsUnloadData[i].cars.Any<Car>((Func<Car, bool>)(car => car.CurrentCargoTypeInCar != carsUnloadData[i].cargoType)))
				{
					if (forceFillCargoIfMissing)
					{
						float totalCargoAmount = carsUnloadData[i].totalCargoAmount;
						foreach (Car car in carsUnloadData[i].cars)
						{
							car.DumpCargo();
							float cargoAmount = (double)totalCargoAmount > (double)car.capacity ? car.capacity : totalCargoAmount;
							car.LoadCargo(cargoAmount, carsUnloadData[i].cargoType, (WarehouseMachine)null);
							totalCargoAmount -= cargoAmount;
						}
					}
					else
						Debug.LogWarning((object)"Initial cargo state on car is not correct. This is valid only when loading save game!");
				}
			}
			List<Task> parallelTasks1 = new List<Task>();
			for (int index = 0; index < carsUnloadData.Count; ++index)
				parallelTasks1.Add((Task)new WarehouseTask(carsUnloadData[index].cars, WarehouseTaskType.Unloading, unloadMachine, carsUnloadData[index].cargoType, carsUnloadData[index].totalCargoAmount, 0L));
			ParallelTasks parallelTasks2 = new ParallelTasks(parallelTasks1, 0L);
			List<Task> parallelTasks3 = new List<Task>();
			for (int index = 0; index < destinationTracksData.Count; ++index)
			{
				TransportTask transportTask2 = JobsGenerator.CreateTransportTask(destinationTracksData[index].cars, destinationTracksData[index].track, unloadMachine.WarehouseTrack, (List<CargoType>)null);
				parallelTasks3.Add((Task)transportTask2);
			}
			ParallelTasks parallelTasks4 = new ParallelTasks(parallelTasks3, 0L);
			DV.Logic.Job.Job job = new DV.Logic.Job.Job((Task)new SequentialTasks(new List<Task>()
				  {
					(Task) transportTask1,
					(Task) parallelTasks2,
					(Task) parallelTasks4
				  }, 0L), JobType.ShuntingUnload, timeLimit, initialWage, chainData, forcedJobId, requiredLicenses);
			jobOriginStation.AddJobToStation(job);
			__result = job;
			return false;
		}
	}

	[HarmonyPatch(typeof(JobsGenerator))]
	[HarmonyPatch("CreateShuntingUnloadJob")]
	class CreateShuntingUnloadJob_Patch
	{
		static bool Prefix(
			out DV.Logic.Job.Job __result,
			Station jobOriginStation,
			StationsChainData chainData,
			Track startingTrack,
			List<CarsPerTrack> destinationTracksData,
			WarehouseMachine unloadMachine,
			List<CarsPerCargoType> carsUnloadData,
			bool forceFillCargoIfMissing = false,
			float timeLimit = 0.0f,
			float initialWage = 0.0f,
			string forcedJobId = null,
			JobLicenses requiredLicenses = JobLicenses.Basic)
		{
			if (destinationTracksData == null || destinationTracksData.Count == 0)
				throw new Exception(string.Format("Error while creating {0} job, {1} is null or empty!", (object)JobType.ShuntingUnload, (object)nameof(destinationTracksData)));
			if (carsUnloadData == null || carsUnloadData.Count == 0)
				throw new Exception(string.Format("Error while creating {0} job, {1} is null or empty!", (object)JobType.ShuntingUnload, (object)nameof(carsUnloadData)));
			List<CargoType> cargoTypePerCar = JobsGenerator.GetCargoTypePerCar(carsUnloadData);
			TransportTask transportTask1 = JobsGenerator.CreateTransportTask(carsUnloadData.SelectMany<CarsPerCargoType, Car>((Func<CarsPerCargoType, IEnumerable<Car>>)(loadData => (IEnumerable<Car>)loadData.cars)).ToList<Car>(), unloadMachine.WarehouseTrack, startingTrack, cargoTypePerCar);
			for (int i = 0; i < carsUnloadData.Count; i++)
			{
				if (carsUnloadData[i].cars.Any<Car>((Func<Car, bool>)(car => !CargoTypes.CanCarContainCargoType(car.carType, carsUnloadData[i].cargoType))))
					throw new Exception(string.Format("Error while creating {0} job, not all cars from {1}[{2}] {4} can carry {3}!", (object)JobType.ShuntingUnload, (object)nameof(carsUnloadData), (object)i, (object)carsUnloadData[i].cargoType, carsUnloadData[i]));
				if ((double)carsUnloadData[i].cars.Select<Car, float>((Func<Car, float>)(car => car.capacity)).Sum() < (double)carsUnloadData[i].totalCargoAmount)
					throw new Exception(string.Format("Error while creating {0} job, {1} {2} to unload is beyond {3}[{4}].cars capacity!", (object)JobType.ShuntingUnload, (object)carsUnloadData[i].totalCargoAmount, (object)carsUnloadData[i].cargoType, (object)nameof(carsUnloadData), (object)i));
				if (!unloadMachine.IsCargoSupported(carsUnloadData[i].cargoType))
					throw new Exception(string.Format("Error while creating {0} job, cargo type we want to unload [{1}] is not supported by {2}", (object)JobType.ShuntingUnload, (object)carsUnloadData[i].cargoType, (object)nameof(unloadMachine)));
				if ((double)carsUnloadData[i].cars.Select<Car, float>((Func<Car, float>)(car => car.LoadedCargoAmount)).Sum() < (double)carsUnloadData[i].totalCargoAmount || carsUnloadData[i].cars.Any<Car>((Func<Car, bool>)(car => car.CurrentCargoTypeInCar != carsUnloadData[i].cargoType)))
				{
					if (forceFillCargoIfMissing)
					{
						float totalCargoAmount = carsUnloadData[i].totalCargoAmount;
						foreach (Car car in carsUnloadData[i].cars)
						{
							car.DumpCargo();
							float cargoAmount = (double)totalCargoAmount > (double)car.capacity ? car.capacity : totalCargoAmount;
							car.LoadCargo(cargoAmount, carsUnloadData[i].cargoType, (WarehouseMachine)null);
							totalCargoAmount -= cargoAmount;
						}
					}
					else
						Debug.LogWarning((object)"Initial cargo state on car is not correct. This is valid only when loading save game!");
				}
			}
			List<Task> parallelTasks1 = new List<Task>();
			for (int index = 0; index < carsUnloadData.Count; ++index)
				parallelTasks1.Add((Task)new WarehouseTask(carsUnloadData[index].cars, WarehouseTaskType.Unloading, unloadMachine, carsUnloadData[index].cargoType, carsUnloadData[index].totalCargoAmount, 0L));
			ParallelTasks parallelTasks2 = new ParallelTasks(parallelTasks1, 0L);
			List<Task> parallelTasks3 = new List<Task>();
			for (int index = 0; index < destinationTracksData.Count; ++index)
			{
				TransportTask transportTask2 = JobsGenerator.CreateTransportTask(destinationTracksData[index].cars, destinationTracksData[index].track, unloadMachine.WarehouseTrack, (List<CargoType>)null);
				parallelTasks3.Add((Task)transportTask2);
			}
			ParallelTasks parallelTasks4 = new ParallelTasks(parallelTasks3, 0L);
			DV.Logic.Job.Job job = new DV.Logic.Job.Job((Task)new SequentialTasks(new List<Task>()
			  {
				(Task) transportTask1,
				(Task) parallelTasks2,
				(Task) parallelTasks4
			  }, 0L), JobType.ShuntingUnload, timeLimit, initialWage, chainData, forcedJobId, requiredLicenses);
			jobOriginStation.AddJobToStation(job);
			__result = job;
			return false;
		}
	}

	[HarmonyPatch(typeof(JobsGenerator))]
	[HarmonyPatch("CreateTransportJob")]
	class CreateTransportJob_Patch
	{
		static bool Prefix(
			out DV.Logic.Job.Job __result,
			Station jobOriginStation,
			StationsChainData chainData,
			List<Car> cars,
			Track destinationTrack,
			Track startingTrack = null,
			List<CargoType> transportedCargoPerCar = null,
			List<float> cargoAmountPerCar = null,
			bool forceFillCargoIfMissing = false,
			float timeLimit = 0.0f,
			float initialWage = 0.0f,
			string forcedJobId = null,
			JobLicenses requiredLicenses = JobLicenses.Basic)
		{
			int num = transportedCargoPerCar != null ? 1 : 0;
			bool flag = cargoAmountPerCar != null;
			if (num != (flag ? 1 : 0))
				throw new Exception("Error while creating transport job, one of transportedCargoPerCar and cargoAmountPerCar is not initialized!");
			if ((num & (flag ? 1 : 0)) != 0)
			{
				if (transportedCargoPerCar.Count != cargoAmountPerCar.Count)
					throw new Exception("Error while creating transport job, transportedCargoPerCar and cargoAmountPerCar count is not matching!");
				for (int index = 0; index < cars.Count; ++index)
				{
					if (!CargoTypes.CanCarContainCargoType(cars[index].carType, transportedCargoPerCar[index]))
						throw new Exception(string.Format("Error while creating transport job, {0}[{1}] {4} can't carry specified {2}[{3}] {5}!", (object)nameof(cars), (object)index, (object)nameof(transportedCargoPerCar), (object)index, cars[index], transportedCargoPerCar[index]));
					if ((double)cars[index].capacity < (double)cargoAmountPerCar[index])
						throw new Exception(string.Format("Error while creating transport job, {0}[{1}] can't fit in {2}[{3}]", (object)nameof(cargoAmountPerCar), (object)index, (object)nameof(cars), (object)index));
					if ((double)cars[index].LoadedCargoAmount < (double)cargoAmountPerCar[index] || cars[index].CurrentCargoTypeInCar != transportedCargoPerCar[index])
					{
						if (!forceFillCargoIfMissing)
							throw new Exception(string.Format("Error while creating transport job, {0}[{1}] doesn't have required {2}!", (object)cars, (object)index, (object)nameof(cargoAmountPerCar)));
						cars[index].DumpCargo();
						cars[index].LoadCargo(cargoAmountPerCar[index], transportedCargoPerCar[index], (WarehouseMachine)null);
					}
				}
			}
			DV.Logic.Job.Job job = new DV.Logic.Job.Job((Task)JobsGenerator.CreateTransportTask(cars, destinationTrack, startingTrack, transportedCargoPerCar), JobType.Transport, timeLimit, initialWage, chainData, forcedJobId, requiredLicenses);
			jobOriginStation.AddJobToStation(job);
			__result = job;
			return false;
		}
	}
}