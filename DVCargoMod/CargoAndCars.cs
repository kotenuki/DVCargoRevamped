using System.Collections.Generic;
using System.Linq;

using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using UnityEngine;

using static DV.ThingTypes.CargoType;
using static DV.ThingTypes.TrainCarType;

namespace DvCargoMod;

#region CargoAndCarLists
static class Cargos
{
    public static List<CargoType> tankCarCargos = new()
    {
        CrudeOil, Diesel, Gasoline,
        Methane, Alcohol,
        Ammonia, SodiumHydroxide,
        Argon, Nitrogen, CryoHydrogen, CryoOxygen,
        ChemicalsIskar, ChemicalsSperex,
    };
    public static List<CargoType> boxcarCargoes = new()
    {
        Boards, Plywood,
        SteelBentPlates, SteelBillets, SteelRails, SteelRolls, SteelSlabs,
        Bread, CatFood, CannedFood, DairyProducts, MeatProducts, Medicine,
        Wheat, Corn,
    };
    public static List<CargoType> containerizedCargos = new()
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
    // cargos that can be put into containers, but aren't in vanilla
    private static List<CargoType> _ContainerizableCargos = new List<CargoType>();
    public static List<CargoType> ContainerizableCargos
    {
        get
        {
            if (_ContainerizableCargos.Count() == 0)
            {
                _ContainerizableCargos.AddRange(containerizableCargosAnyContainer);
                _ContainerizableCargos.AddRange(containerizableCargosIsoOxydizing);
                _ContainerizableCargos.AddRange(containerizableCargosIsoExplosive);
            }
            return _ContainerizableCargos;
        }
    }

    public static List<CargoType> containerizableCargosAnyContainer = new()
    {
        Wheat, Corn, // yes it's a thing: https://pittoship.com/containers/grain-shipping-containers/
        Bread,
        ScrapMetal,
        Pipes,
        NewCars, ImportedNewCars,
    };
    public static List<CargoType> containerizableCargosIsoOxydizing = new()
    {
        Ammonia, SodiumHydroxide,
    };
    public static List<CargoType> containerizableCargosIsoExplosive = new()
    {
        Alcohol,
    };
    public static List<CargoType> perishableCargos = new()
    {
        Bread, CatFood, CannedFood, DairyProducts, MeatProducts,
        Medicine,
        Chickens, Cows, Goats, Pigs, Sheep,
    };
    public static List<CargoType> bulkCargos = new()
    {
        CargoType.Coal, IronOre,
        Logs,
        ScrapMetal
    };
}

static class Cars
{
    public static List<TrainCarType> fluidCars = new()
    {
		// FlatbedEmpty,
		TankWhite, // TankYellow, TankChrome, // TankOil
		TankBlue, // TankOrange,  // TankGas
		TankBlack, // TankChem
	};
    public static List<TrainCarType> nonPerishableCars = new()
    {
        FlatbedEmpty,
        BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
        GondolaGray, GondolaGreen, GondolaRed,
    };
    public static List<TrainCarType> perishableCars = new()
    {
        FlatbedEmpty,
        BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
        RefrigeratorWhite,
    };
    public static List<TrainCarType> bulkCars = new()
    {
        HopperBrown, HopperTeal, HopperYellow,
        GondolaGray, GondolaGreen, GondolaRed,
    };
    public static List<TrainCarType> tankers = new()
    {
        TankWhite, TankYellow, TankChrome,
        TankBlue, TankOrange,
        TankBlack,
    };
    public static List<TrainCarType> boxcars = new()
    {
        BoxcarBrown, BoxcarGreen, BoxcarPink, BoxcarRed,
        RefrigeratorWhite
    };
    public static List<TrainCarType> flatcars = new()
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
    public static TrainCarType_v2 FlatbedStakes { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "FlatbedStakes").First();
    public static TrainCarType_v2 Gondola { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Gondola").First();
    public static TrainCarType_v2 Hopper { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Hopper").First();
    public static TrainCarType_v2 Refrigerator { get; } = DV.Globals.G.Types.carTypes.Where(tct => tct.id == "Refrigerator").First();
}

static class LoadableInfos
{
    public static GameObject[] GondolaScrap { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "ScrapMetal").First().GetCargoPrefabsForCarType(TCT.Gondola);
    public static class Tankers
    {
        public static GameObject[] TankFlammable { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "CrudeOil").First().GetCargoPrefabsForCarType(TCT.TankOil);
        public static GameObject[] TankExplosive { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "Gasoline").First().GetCargoPrefabsForCarType(TCT.TankGas);
        public static GameObject[] TankCorrosive { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "Ammonia").First().GetCargoPrefabsForCarType(TCT.TankChem);
    }
    public static class Containers
    {
        private static GameObject[] _AllContainers = new GameObject[] { };
        public static GameObject[] AllContainers
        {
            get
            {
                if (_AllContainers.Length == 0)
                {
                    var l = _AllContainers.ToList();
                    l.AddRange(AllHazmatContainers);
                    l.AddRange(AllNormalContainers);
                    _AllContainers = l.ToArray();
                }
                return _AllContainers;
            }
        }
        private static GameObject[] _AllHazmatContainers = new GameObject[] { };
        public static GameObject[] AllHazmatContainers
        {
            get
            {
                if (_AllHazmatContainers.Length == 0)
                {
                    var l = _AllHazmatContainers.ToList();
                    l.AddRange(Hazmat.Asphyxiating);
                    l.AddRange(Hazmat.Explosive);
                    l.AddRange(Hazmat.Oxydizing);
                    l.AddRange(Hazmat.Acetylene);
                    _AllHazmatContainers = l.ToArray();
                }
                return _AllHazmatContainers;
            }
        }

        public static GameObject[] _AllNormalContainers = new GameObject[] { };
        public static GameObject[] AllNormalContainers
        {
            get
            {
                if (_AllNormalContainers.Length == 0)
                {
                    var l = _AllNormalContainers.ToList();
                    l.AddRange(Normal.AAG);
                    l.AddRange(Normal.Brohm);
                    l.AddRange(Normal.ChemlekAC);
                    l.AddRange(Normal.Goorsk);
                    l.AddRange(Normal.Iskar);
                    l.AddRange(Normal.Krugmann);
                    l.AddRange(Normal.NeoGamma);
                    l.AddRange(Normal.Novae);
                    l.AddRange(Normal.Obco);
                    l.AddRange(Normal.Sperex);
                    l.AddRange(Normal.SunOmni);
                    l.AddRange(Normal.Traeg);
                    _AllNormalContainers = l.ToArray();
                }
                return _AllNormalContainers;
            }
        }
        public static class Hazmat
        {
            public static GameObject[] Asphyxiating { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "Argon").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Explosive { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "CryoHydrogen").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Oxydizing { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "CryoOxygen").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Acetylene { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "Acetylene").First().GetCargoPrefabsForCarType(TCT.Flatbed);
        }
        public static class Normal
        {
            private static GameObject[] _FoodContainers = new GameObject[] { };
            public static GameObject[] FoodContainers
            {
                get
                {
                    if (_FoodContainers.Length == 0)
                    {
                        var l = _FoodContainers.ToList();
                        l.AddRange(SunOmni);
                        _FoodContainers = l.ToArray();
                    }
                    return _FoodContainers;
                }
            }
            public static GameObject[] AAG { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyAAG").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Brohm { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyBrohm").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] ChemlekAC { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyChemlek").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Goorsk { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyGoorsk").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Iskar { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyIskar").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Krugmann { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyKrugmann").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] NeoGamma { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyNeoGamma").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Novae { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyNovae").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Obco { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyObco").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Sperex { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptySperex").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] SunOmni { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptySunOmni").First().GetCargoPrefabsForCarType(TCT.Flatbed);
            public static GameObject[] Traeg { get; } = DV.Globals.G.Types.cargos.Where(c => c.id == "EmptyTraeg").First().GetCargoPrefabsForCarType(TCT.Flatbed);
        }
    }
}
#endregion