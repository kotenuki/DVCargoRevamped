using System.Collections.Generic;
using System.Linq;

using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;

using UnityEngine;

using static DV.ThingTypes.CargoType;
using static DV.ThingTypes.TrainCarType;

namespace DvCargoMod;

#region CargoAndCarLists
static class Cargos
{
    public static List<CargoType> tankCarCargos = new List<CargoType>
    {
        CrudeOil, Diesel, Gasoline,
        Methane, Alcohol,
        Ammonia, SodiumHydroxide,
        Argon, Nitrogen, CryoHydrogen, CryoOxygen,
        ChemicalsIskar, ChemicalsSperex,
    };

    public static List<CargoType> boxcarCargoes = new List<CargoType>
    {
        Boards, Plywood,
        SteelBentPlates, SteelBillets, SteelRails, SteelRolls, SteelSlabs,
        Bread, CatFood, CannedFood, DairyProducts, MeatProducts, Medicine,
    };

    public static List<CargoType> containerizedCargos = new List<CargoType>
    {
        ElectronicsAAG, ElectronicsIskar, ElectronicsKrugmann, ElectronicsNovae, ElectronicsTraeg,
        ClothingNeoGamma, ClothingNovae, ClothingObco, ClothingTraeg,
        ToolsIskar, ToolsBrohm, ToolsAAG, ToolsNovae, ToolsTraeg,
        ChemicalsIskar, ChemicalsSperex,
        Argon, Nitrogen, CryoHydrogen, CryoOxygen,
        Bread, CatFood, CannedFood, DairyProducts, MeatProducts, Medicine,
        EmptySunOmni, EmptyIskar, EmptyObco, EmptyGoorsk, EmptyKrugmann, EmptyBrohm,
        EmptyAAG, EmptySperex, EmptyNovae, EmptyTraeg, EmptyChemlek, EmptyNeoGamma,
    };

    public static List<CargoType> perishableCargos = new List<CargoType>
    {
        Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
        Medicine,
        Chickens, Cows, Goats, Pigs, Sheep,
    };

    public static List<CargoType> bulkCargos = new List<CargoType>
    {
        CargoType.Coal, IronOre,
        Logs,
        ScrapMetal
    };
}

static class Cars
{
    public static List<TrainCarType_v2> fluidCars = new List<TrainCarType>
    {
		// FlatbedEmpty,
		TankWhite, // TankYellow, TankChrome, // TankOil
		TankBlue, // TankOrange,  // TankGas
		TankBlack, // TankChem
	}.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

    public static List<TrainCarType_v2> nonPerishableCars = new List<TrainCarType>
    {
        FlatbedEmpty,
        BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
        GondolaGray, GondolaGreen, GondolaRed,
    }.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

    public static List<TrainCarType_v2> perishableCars = new List<TrainCarType>
    {
        FlatbedEmpty,
        BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
        RefrigeratorWhite,
    }.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

    public static List<TrainCarType_v2> bulkCars = new List<TrainCarType>
    {
        HopperBrown, HopperTeal, HopperYellow,
        GondolaGray, GondolaGreen, GondolaRed,
    }.ConvertAll(c => c.ToV2().parentType).Distinct().ToList();

    public static List<TrainCarType> tankers = new List<TrainCarType>
    {
        TankWhite, TankYellow, TankChrome,
        TankBlue, TankOrange,
        TankBlack,
    };

    public static List<TrainCarType> boxcars = new List<TrainCarType>
    {
        BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
        RefrigeratorWhite
    };

    public static List<TrainCarType> flatcars = new List<TrainCarType>
    {
        FlatbedEmpty
    };
}

class TCT
{
    public static TrainCarType_v2 TankChem { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "TankChem").First();
    public static TrainCarType_v2 TankGas { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "TankGas").First();
    public static TrainCarType_v2 TankOil { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "TankOil").First();
    public static TrainCarType_v2 Boxcar { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Boxcar").First();
    public static TrainCarType_v2 Flatbed { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Flatbed").First();
    public static TrainCarType_v2 Gondola { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Gondola").First();
    public static TrainCarType_v2 Hopper { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Hopper").First();
    public static TrainCarType_v2 Refrigerator { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Refrigerator").First();
}

static class LoadableInfos
{
    public static GameObject[] TankFlammable { get; } = CrudeOil.ToV2().GetCargoPrefabsForCarType(TCT.TankOil);
    public static GameObject[] TankExplosive { get; } = Gasoline.ToV2().GetCargoPrefabsForCarType(TCT.TankGas);
    public static GameObject[] TankCorrosive { get; } = Ammonia.ToV2().GetCargoPrefabsForCarType(TCT.TankChem);
    public static GameObject[] FlatcarAsph { get; } = Argon.ToV2().GetCargoPrefabsForCarType(TCT.Flatbed);
    public static GameObject[] FlatcarExplosive { get; } = CryoHydrogen.ToV2().GetCargoPrefabsForCarType(TCT.Flatbed);
    public static GameObject[] FlatcarOxy { get; } = CryoOxygen.ToV2().GetCargoPrefabsForCarType(TCT.Flatbed);
    public static GameObject[] GondolaScrap { get; } = ScrapMetal.ToV2().GetCargoPrefabsForCarType(TCT.Gondola);
}
#endregion