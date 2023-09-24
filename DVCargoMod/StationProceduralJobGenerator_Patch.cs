using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DV;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DvCargoMod;
using HarmonyLib;
using static StationProceduralJobGenerator;

[HarmonyPatch(typeof(StationProceduralJobGenerator), "GenerateBaseCargoTrainData")]
class StationProceduralJobGenerator_GenerateBaseCargoTrainData_Patch
{
    static void Postfix(
        StationProceduralJobGenerator __instance,
        ref List<CarTypesPerCargoTypeData> __result,
        [HarmonyArgument("allCarLiveries")] ref List<TrainCarLivery> allCarLiveries
    )
    {
        // Main.DebugLog(LoggingLevel.Debug, () => $"\nresult: [{__result.Select(c => $"{c.cargoType}:[{c.carTypes.Select(l => l.v1).Join()}]").Join()}]");
        // Main.DebugLog(LoggingLevel.Debug, () => $"cargoTypes: {cargoTypes.Select(c => c.ToString()).Join()}");
        // Main.DebugLog(LoggingLevel.Debug, () => $"allCarLiveries: {allCarLiveries.Select(l => l.id).Join()}");

        // change train car type and livery
        var output = new List<CarTypesPerCargoTypeData>();
        foreach (var info in __result)
        {
            var cargo = info.cargoType;
            var liveries = new List<TrainCarLivery>();
            foreach (var _ in info.carTypes)
            {
                var tct = __instance.GetRandomFromList<TrainCarType_v2>(Globals.G.Types.CargoToLoadableCarTypes[cargo.ToV2()]);
                liveries.Add(__instance.GetRandomFromList<TrainCarLivery>(tct.liveries));
            }
            output.Add(new CarTypesPerCargoTypeData(liveries, cargo, info.totalCargoAmount));
        }
        __result = output;
        // update livery list
        allCarLiveries = __result.SelectMany(info => info.carTypes).ToList();
    }
}
