using System;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DvCargoMod;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(JobsGenerator), nameof(JobsGenerator.CreateShuntingLoadJob))]
class JobsGenerator_CreateShuntingLoadJob_Patch
{
    static void Prefix(
        [HarmonyArgument("carsLoadData")] List<CarsPerCargoType> carsLoadData
    )
    {
        for (int index = 0; index < carsLoadData.Count; ++index)
        {
            CargoType_v2 cargoV2 = carsLoadData[index].cargoType.ToV2();
            if (carsLoadData[index].cars.Any<Car>((Func<Car, bool>)(car => !cargoV2.IsLoadableOnCarType(car.carType.parentType))))
            {
                var s = $"[{Main.mod?.Info.Id}] Error while creating {JobType.ShuntingLoad} job, not all cars from {nameof(carsLoadData)}[{index}] ({carsLoadData[index].cars.Select(c => $"{c.carType.parentType.id}:{c.carType.id}").Join()}) can carry {carsLoadData[index].cargoType}!";
                Main.ErrorLog(() => s);
                break;
            }
        }
    }
}

[HarmonyPatch(typeof(JobsGenerator), nameof(JobsGenerator.CreateShuntingUnloadJob))]
class JobsGenerator_CreateShuntingUnloadJob_Patch
{
    static void Prefix(
        [HarmonyArgument("carsUnloadData")] List<CarsPerCargoType> carsUnloadData
    )
    {
        for (int index = 0; index < carsUnloadData.Count; index++)
        {
            CargoType_v2 cargoV2 = carsUnloadData[index].cargoType.ToV2();
            if (carsUnloadData[index].cars.Any<Car>((Func<Car, bool>)(car => !cargoV2.IsLoadableOnCarType(car.carType.parentType))))
            {
                var s = $"[{Main.mod?.Info.Id}] Error while creating {JobType.ShuntingUnload} job, not all cars from {nameof(carsUnloadData)}[{index}] ({carsUnloadData[index].cars.Select(c => $"{c.carType.parentType.id}:{c.carType.id}").Join()}) can carry {carsUnloadData[index].cargoType}!";
                Main.ErrorLog(() => s);
                break;
            }
        }
    }
}

[HarmonyPatch(typeof(JobsGenerator), nameof(JobsGenerator.CreateTransportJob))]
class JobsGenerator_CreateTransportJob_Patch
{
    static void Prefix(
        [HarmonyArgument("cars")] List<Car> cars,
        [HarmonyArgument("transportedCargoPerCar")] List<CargoType> transportedCargoPerCar,
        [HarmonyArgument("cargoAmountPerCar")] List<float> cargoAmountPerCar
    )
    {
        bool cargoPerCarNotNull = transportedCargoPerCar != null;
        bool cargoAmountNotNull = cargoAmountPerCar != null;
        if (cargoPerCarNotNull != cargoAmountNotNull)
            throw new Exception("Error while creating transport job, one of transportedCargoPerCar and cargoAmountPerCar is not initialized!");
        if (cargoPerCarNotNull && cargoAmountNotNull)
        {
            if (transportedCargoPerCar!.Count != cargoAmountPerCar!.Count)
                throw new Exception("Error while creating transport job, transportedCargoPerCar and cargoAmountPerCar count is not matching!");
            for (int index = 0; index < cars.Count; ++index)
            {
                if (!transportedCargoPerCar![index].ToV2().IsLoadableOnCarType(cars[index].carType.parentType))
                {
                    var s = $"[{Main.mod?.Info.Id}] Error while creating transport job, {nameof(cars)}[{index}] ({cars[index].carType.parentType.id}:{cars[index].carType.id}) can't carry specified {nameof(transportedCargoPerCar)}[{index}] ({transportedCargoPerCar[index]})!";
                    Main.ErrorLog(() => s);
                    break;
                }
            }
        }
    }
}