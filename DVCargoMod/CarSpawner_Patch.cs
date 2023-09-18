using System.Collections;
using System.Collections.Generic;
using DV.ThingTypes;
using DV.Utils;
using DvCargoMod;
using HarmonyLib;
using UnityEngine;
using static CarSpawner;

[HarmonyPatch(typeof(CarSpawner), "InitPoolCoro")]
static class CarSpawner_InitPoolCoro_Patch
{
    static bool Prefix()
    {
        return Main.SKIP_ORIGINAL;
    }
    static IEnumerator Postfix(IEnumerator result)
    {
        yield return null;
        if ((bool)SingletonBehaviour<WorldMover>.Instance)
        {
            while (SingletonBehaviour<WorldMover>.Instance.originShiftParent == null)
            {
                yield return null;
            }
        }
        float num = 0f;
        float startingOffsetX = 0f;
        List<TrainCar> spawnedCarsForPool = new List<TrainCar>();
        PooledCarTypeSetup[] array = CarSpawner.Instance.poolSetup;
        foreach (PooledCarTypeSetup carTypeSetup in array)
        {
            foreach (TrainCarLivery livery in carTypeSetup.carType.liveries)
            {
                if (CarTypes.IsAnyLocomotiveOrTender(livery))
                {
                    Debug.LogError("Unexpected state: Locos [" + livery.id + "] can't be pooled currently, skipping.");
                    continue;
                }
                GameObject prefab = livery.prefab;
                if (prefab == null)
                {
                    Debug.LogError("Unexpected state: Pooled car livery " + livery.id + " has null for prefab, ignoring pooling.");
                    continue;
                }
                List<TrainCar> value = new List<TrainCar>();
                var dict = Traverse.Create(CarSpawner.Instance).Field("carLiveryToTrainCarPool").GetValue<Dictionary<TrainCarLivery, List<TrainCar>>>();
                if (!dict.ContainsKey(livery))
                {
                    dict.Add(livery, value);
                }
                for (int k = 0; k < carTypeSetup.numberOfPooledInstancesPerLivery; k++)
                {
                    TrainCar component = UnityEngine.Object.Instantiate(prefab, new Vector3(startingOffsetX, -2000f, num), Quaternion.identity).GetComponent<TrainCar>();
                    component.rb.isKinematic = true;
                    num += 30f;
                    spawnedCarsForPool.Add(component);
                }
                yield return WaitFor.EndOfFrame;
                foreach (TrainCar item in spawnedCarsForPool)
                {
                    CarSpawner.Instance.ReturnToPool(item);
                }
                spawnedCarsForPool.Clear();
                for (int i = 0; i < 2; i++)
                {
                    yield return null;
                }
                startingOffsetX += 10f;
                num = 0f;
            }
        }
        Traverse.Create(CarSpawner.Instance).Field("poolInitialized").SetValue(true);
        Debug.Log("Car pool initialized.");
    }
}