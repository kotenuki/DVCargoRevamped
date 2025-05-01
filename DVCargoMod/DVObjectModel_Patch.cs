using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DvCargoMod;
using HarmonyLib;
using UnityEngine;

using TCCDP = TrainCarAndCargoDamageProperties;

[HarmonyPatch(typeof(DVObjectModel), nameof(DVObjectModel.RecalculateCaches))]
[HarmonyPriority(Priority.First)]
class DVObjectModel_RecalculateCaches_Patch
{
    static bool Prefix(
        ref DVObjectModel __instance
    )
    {

        return Main.KEEP_ORIGINAL;
    }

    static void Postfix(
        ref DVObjectModel __instance,
        ref Dictionary<TrainCarType_v2, List<CargoType_v2>> ____carTypeToLoadableCargo,
        ref Dictionary<CargoType_v2, List<TrainCarType_v2>> ____cargoToLoadableCarTypes
    )
    {
        Main.DebugLog(() => "Recalculating caches");
        // link liveries to traincartypes
        // __instance.carTypes = UpdateTrainCars(ref __instance);
        // link traincartypes to cargos
        __instance.cargos = UpdateCargos(ref __instance);

        // recalculate dicts
        var cargos = __instance.cargos;
        var carTypes = __instance.carTypes;

        Main.DebugLog(() => "Reloading mappings");
        Main.DebugLog(LoggingLevel.Verbose, () => "Reloading _carTypeToLoadableCargo");
        ____carTypeToLoadableCargo = __instance.carTypes.ToDictionary(c => c, c => cargos.Where(cg => cg.loadableCarTypes.Any(lct => lct.carType == c)).ToList());
        Main.DebugLog(LoggingLevel.Verbose, () => "Reloading _cargoToLoadableCarTypes");
        ____cargoToLoadableCarTypes = cargos.ToDictionary(c => c, c => c.loadableCarTypes.Select(lct => lct.carType).ToList());
        Main.DebugLog(() => "Finished reloading mappings");

        if (Main.settings.loggingLevel != LoggingLevel.None)
        {
            Main.DebugLog(LoggingLevel.Debug, () => "\nAFTER DICT RELOAD");
            foreach (var cargo in cargos)
            {
                Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} carTypes: [{cargo.loadableCarTypes.Select(info => info.carType.id).Join()}]");
                Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} liveries: [{cargo.loadableCarTypes.SelectMany(info => info.carType.liveries).Select(l => $"{l.id} ({l.parentType.id})").Distinct().Join()}]");
                Main.DebugLog(LoggingLevel.Debug, () => $"{cargo.id} prefabs:  [{carTypes.SelectMany(ct => { var gos = cargo.GetCargoPrefabsForCarType(ct); return gos == null ? new List<GameObject>() : gos.ToList(); }).Select(go => go.name).Join()}]");
            }
        }
        Main.DebugLog(() => "Caches recalculated");
    }

    private static List<TrainCarType_v2> UpdateTrainCars(ref DVObjectModel instance)
    {
        Main.DebugLog(() => "Adding TrainCarLiveries to TrainCarTypes");
        var carTypes = new List<TrainCarType_v2>();
        foreach (var carType in instance.carTypes)
        {
            if (carType.id.Contains("Tank"))
            {
                Main.DebugLog(LoggingLevel.Verbose, () => $"Adding tankers to {carType.id}");
                carType.liveries.AddRange(Cars.tankers.ConvertAll(t => t.ToV2()));
            }
            else if (!carType.id.Contains("Military") && carType.id.Contains("Boxcar") || carType.id.Contains("Refrigerator"))
            {
                Main.DebugLog(LoggingLevel.Verbose, () => $"Adding boxcars to {carType.id}");
                carType.liveries.AddRange(Cars.boxcars.ConvertAll(t => t.ToV2()));
            }
            else if (!carType.id.Contains("Military") && carType.id == "Flatbed")
            {
                carType.liveries.AddRange(Cars.boxcars.ConvertAll(t => t.ToV2()));
            }
            carType.liveries = carType.liveries.Distinct().ToList();
            carTypes.Add(carType);
        }
        Main.DebugLog(() => "Completed adding TrainCarLiveryies to TrainCarTypes");
        return carTypes;
    }

    private static List<CargoType_v2> UpdateCargos(ref DVObjectModel instance)
    {
        Main.DebugLog(() => "Adding LoadableInfos to CargoType_v2s");
        var cargos = new List<CargoType_v2>();
		var comparer = new LoadableInfoComparer();
        foreach (var cargo in instance.cargos)
        {
            // add fluids and gasses to tank cars
            if (Cargos.tankCarCargos.Contains(cargo.v1))
            {
                Main.DebugLog(LoggingLevel.Verbose, () => $"Adding tankers to {cargo.v1}");
                var tankerLiveries = Cars.tankers;
                var tankerTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => tankerLiveries.Contains(l.v1)));

                var tankerPrefab = new GameObject[] { };
                if (TCCDP.IsCargoFlammable(cargo.v1))
                {
                    tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankOil);
                    tankerPrefab = LoadableInfos.Tankers.TankFlammable;
                }
                else if (TCCDP.IsCargoExplosive(cargo.v1))
                {
                    tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankGas);
                }
                else if (TCCDP.IsCargoCorrosiveLiquid(cargo.v1))
                {
                    tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankChem);
                }
				else
				{
					tankerPrefab = cargo.GetCargoPrefabsForCarType(TCT.TankMilk);
				}
				var tankerInfo = tankerTypes.Select(t => new CargoType_v2.LoadableInfo(t, tankerPrefab));
                var loadables = cargo.loadableCarTypes.Distinct(comparer).ToList();
                loadables.AddRange(tankerInfo);
                cargo.loadableCarTypes = loadables.Distinct(comparer).ToArray();
				var cargoToCargoPrefabs = new Dictionary<TrainCarType_v2, GameObject[]>();
				foreach (var loadable in cargo.loadableCarTypes)
				{
					cargoToCargoPrefabs.Add(loadable.carType, loadable.cargoPrefabVariants);
				}
				Traverse.Create(cargo).Field("_trainCargoToCargoPrefabs").SetValue(cargoToCargoPrefabs);
			}

            // add (certain) flatcar cargoes to boxcars
            if (Cargos.boxcarCargoes.Contains(cargo.v1))
            {
                Main.DebugLog(LoggingLevel.Verbose, () => $"Adding boxcars to {cargo.v1}");
                var boxcarLiveries = Cars.boxcars;
                var boxcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => boxcarLiveries.Contains(l.v1)));
                var boxcarPrefab = cargo.GetCargoPrefabsForCarType(TCT.Boxcar);
                var boxcarInfo = boxcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, boxcarPrefab));

				var loadables = cargo.loadableCarTypes.ToList();
				loadables.AddRange(boxcarInfo);
                cargo.loadableCarTypes = loadables.Distinct(comparer).ToArray();
				var cargoToCargoPrefabs = new Dictionary<TrainCarType_v2, GameObject[]>();
				foreach (var loadable in cargo.loadableCarTypes)
				{
					cargoToCargoPrefabs.Add(loadable.carType, loadable.cargoPrefabVariants);
				}
				Traverse.Create(cargo).Field("_trainCargoToCargoPrefabs").SetValue(cargoToCargoPrefabs);
			}

            // add stake flats to already-containerized cargoes
            if (Cargos.containerizedCargos.Contains(cargo.v1))
            {
                Main.DebugLog(LoggingLevel.Verbose, () => $"Adding staked flatcars to pre-containerized cargo {cargo.v1}");
				var flatcarLiveries = Cars.flatcars;
                var flatcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => flatcarLiveries.Contains(l.v1)));
                var flatcarPrefab = cargo.GetCargoPrefabsForCarType(TCT.Flatbed);
                if (flatcarPrefab != null)
                {
                    Main.DebugLog(LoggingLevel.Debug, () => $"prefabs for {cargo.v1}: [{flatcarPrefab.Select(pf => pf.name).Join()}]");
                }

				var flatcarInfo = flatcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, flatcarPrefab));
                var loadables = cargo.loadableCarTypes.ToList();
                loadables.AddRange(flatcarInfo);
                cargo.loadableCarTypes = loadables.Distinct(comparer).ToArray();
				var cargoToCargoPrefabs = new Dictionary<TrainCarType_v2, GameObject[]>();
				foreach (var loadable in cargo.loadableCarTypes)
				{
					cargoToCargoPrefabs.Add(loadable.carType, loadable.cargoPrefabVariants);
				}
				Traverse.Create(cargo).Field("_trainCargoToCargoPrefabs").SetValue(cargoToCargoPrefabs);
            }

            // add some containerizable cargoes to flat cars in containers
            if (Cargos.ContainerizableCargos.Contains(cargo.v1))
            {
                Main.DebugLog(LoggingLevel.Verbose, () => $"Adding flatcars to containerizable cargo {cargo.v1}");
                var flatcarLiveries = Cars.flatcars;
                var flatcarTypes = instance.carTypes.Where(carType => carType.liveries.Any(l => flatcarLiveries.Contains(l.v1)));
                var flatcarPrefab = LoadableInfos.Containers.AllNormalContainers;
                if (Cargos.containerizableCargosIsoOxydizing.Contains(cargo.v1))
                {
                    flatcarPrefab = LoadableInfos.Containers.Hazmat.Oxydizing;
                }
                else if (Cargos.containerizableCargosIsoExplosive.Contains(cargo.v1))
                {
                    flatcarPrefab = LoadableInfos.Containers.Hazmat.Explosive;
                }
                if (flatcarPrefab != null)
                {
                    Main.DebugLog(LoggingLevel.Debug, () => $"prefabs for {cargo.v1}: [{flatcarPrefab.Select(pf => pf.name).Join()}]");
                }

                var flatcarInfo = flatcarTypes.Select(t => new CargoType_v2.LoadableInfo(t, flatcarPrefab));
                var loadables = cargo.loadableCarTypes.ToList();
                loadables.AddRange(flatcarInfo);
                cargo.loadableCarTypes = loadables.Distinct(comparer).ToArray();
				var cargoToCargoPrefabs = new Dictionary<TrainCarType_v2, GameObject[]>();
				foreach (var loadable in cargo.loadableCarTypes)
				{
					cargoToCargoPrefabs.Add(loadable.carType, loadable.cargoPrefabVariants);
				}
				Traverse.Create(cargo).Field("_trainCargoToCargoPrefabs").SetValue(cargoToCargoPrefabs);
			}

            cargo.loadableCarTypes = cargo.loadableCarTypes.Distinct(comparer).ToArray();
            cargos.Add(cargo);
        }

        Main.DebugLog(() => "Completed LoadableInfos to CargoType_v2s");
        return cargos;
    }
	class LoadableInfoComparer : IEqualityComparer<CargoType_v2.LoadableInfo>
	{
		public bool Equals(CargoType_v2.LoadableInfo? l1, CargoType_v2.LoadableInfo? l2)
		{
			if (ReferenceEquals(l1, l2)) return true;

			if (l1 is null || l2 is null) return false;

			return l1.carType.liveries == l2.carType.liveries;
		}

		public int GetHashCode(CargoType_v2.LoadableInfo l) => l.carType.liveries.Count;
	}
}
