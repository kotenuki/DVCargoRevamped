# Derail Valley Code Notes

## DV.Logic.Job

### CargoContainerType.cs

* Enum holding the different cars. Note `TankerOil`, `TankerGas`, and `TankerChem` (oil, gas, and chem tankers).

### CargoType.cs

* An enum containing all cargo types in the game.

### CargoTypes.cs

* Classifies some `CargoType`s into their license categories (hazmat and military).
  * `Hazmat[1-3]Cargo`, `Military1CarContainers`, and `Military[2-3]Cargo`
* `CarTypeToContainerType` maps `TrainCarType, CargoContainerType`.
  * Is a dictionary, so can't have duplicate keys.
* `cargoTypeToSupportedCarContainer` maps cargoes to a list of cars that can carry them.
  * Does this relate to `CarTypeToContainerType`? Can these lists be appended to?
* `cargoTypeToCargoMassPerUnit` maps cargoes to their masses
* `cargoSpecificDisplayName` maps cargoes to their full names
* `cargoShortDisplayName` same as above but short names (like on job descriptions?)
* `Dictionary<CargoContainerType, List<TrainCarType>> ContainerTypeToCarTypes.get()`

```md
If the dictionary is not set, set it:
    For each entry in the `CarTypeToContainerType` dictionary,
        If the `CargoContainerType` is not in the dictionary, add an empty `List<TrainCarType>` to the dictionary
    Add to the `CargoContainerType`'s list the `TrainCarType`
At the end, return the dictionary.
```

* `bool CanCarContainCargoType(TrainCarType carType, CargoType cargoType)` checks to see if a given `carType` can hold a given `cargoType` by checking `cargoTypeToSupportedCarContainer`.
* `List<CargoContainerType> GetCarContainerTypesThatSupportCargoType(CargoType cargoType)` returns a list of containers that can hold a given `cargoType`
* `List<TrainCarType> GetTrainCarTypesThatAreSpecificContainerType(CargoContainerType carContainerType)` returns a list of cars that are a certain container
* Then a bunch of getter methods