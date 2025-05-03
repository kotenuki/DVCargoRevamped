using DV.RenderTextureSystem.BookletRender;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using DV.Localization;
using DvCargoMod;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[HarmonyPatch(typeof(TaskTemplatePaper), nameof(TaskTemplatePaper.FillInData))]
class TaskTemplatePaper_Patch
{
	static bool Prefix(ref TaskTemplatePaper __instance)
	{
		if (__instance.data == null)
		{
			Debug.LogWarning("Trying to fill data for task page, but data was not set!", __instance);
			return Main.KEEP_ORIGINAL;
		}
		__instance.stepNum.text = LocalizationAPI.L("job/task_step_no", __instance.data.stepNum);
		__instance.taskType.text = __instance.data.taskType;
		__instance.taskDescription.text = __instance.data.taskDescription;
		__instance.stationName.text = __instance.data.stationName;
		__instance.stationType.text = __instance.data.stationType;
		__instance.yardId.text = __instance.data.yardId;
		__instance.trackId.text = __instance.data.trackId;
		if (__instance.stationName.text == "")
		{
			__instance.yardBgColor.color = __instance.data.yardColor;
			__instance.trackBgColor.color = __instance.data.trackColor;
			__instance.stationName.transform.parent.gameObject.SetActive(false);
			__instance.yardId.transform.parent.gameObject.SetActive(true);
		}
		else
		{
			__instance.stationBgColor.color = __instance.data.stationColor;
			__instance.yardId.transform.parent.gameObject.SetActive(false);
			__instance.stationName.transform.parent.gameObject.SetActive(true);
		}
		__instance.pageNumber.text = __instance.data.pageNumber + "/" + __instance.data.totalPages;
		int count = __instance.data.cars.Count;
		__instance.tracks[0].enabled = true;
		for (int i = 1; i < __instance.tracks.Length; i++)
		{
			__instance.tracks[i].enabled = (count > i * 6);
		}
		bool flag = __instance.data.cargoTypePerCar != null;
		if (flag && __instance.data.cargoTypePerCar?.Count != __instance.data.cars.Count)
		{
			flag = false;
			Debug.LogError("Different number of cargoTypePerCar and cars! This shouldn't happen ever! Will treat it like there is no cargo!");
		}
		TrainCar[] cars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
		for (int j = 0; j < count; j++)
		{
			int num = j % 6;
			int num2 = j / 6;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.car, new Vector3(-56f + 166f * (float)num, 40f, 0f), Quaternion.identity);
			gameObject.transform.SetParent(__instance.tracks[num2].transform, false);
			TrainCarLivery type = __instance.data.cars[j].type;
			Sprite icon = type.icon;
			if (icon != null)
			{
				gameObject.GetComponent<Image>().sprite = icon;
			}
			gameObject.GetComponentInChildren<TextMeshProUGUI>().text = __instance.data.cars[j].ID;
			if (flag)
			{
				CargoType_v2 cargoType_v = __instance.data.cargoTypePerCar![j].ToV2();
				if (cargoType_v.HasVisibleModelForCarType(type.parentType))
				{
					var changeIcon = false;
					if (type.v1 == TrainCarType.FlatbedEmpty || type.v1 == TrainCarType.FlatbedStakes)
					{
						foreach (var cargo in Cargos.ContainerizableCargos)
						{
							if (cargo == __instance.data.cargoTypePerCar[j])
							{
								changeIcon = true;
								break;
							}
						}
					}
					Sprite icon2;
					if (changeIcon)
					{
						var carID = __instance.data.cars[j].ID;
						TrainCar trainCar = cars.Where(c => c.ID == carID).First();
						if (trainCar == null)
						{
							return Main.KEEP_ORIGINAL;
						}
						if (trainCar.CargoModelController.currentCargoModel == null)
						{
							return Main.KEEP_ORIGINAL;
						}
						var currentCargoModelName = trainCar.CargoModelController.currentCargoModel.name;
						if (currentCargoModelName.Contains("Crane")) return Main.KEEP_ORIGINAL;
						if (currentCargoModelName.Contains("Pipes")) return Main.KEEP_ORIGINAL;
						if (currentCargoModelName.Contains("Steel")) return Main.KEEP_ORIGINAL;
						string searchName = currentCargoModelName.Remove(0, 19).Replace("(Clone)", "").Replace("White", "AAG").Replace("Old", "").Replace("AC", "").Replace("Red", "Traeg");
						icon2 = CargoSprites.getContainerSprite(searchName) ?? cargoType_v.icon;
					}
					else
					{
						icon2 = cargoType_v.icon;
					}
					if (icon2 != null)
					{
						Transform transform = gameObject.transform.Find("[cargo icon]");
						if (transform)
						{
							Image component = transform.GetComponent<Image>();
							component.sprite = icon2;
							component.color = Color.white;
						}
						else
						{
							Debug.LogError("Couldn't find cargo icon GO with name [cargo icon]! Skipping cargo icon placement");
						}
					}
				}
			}
			__instance.dynamicallyCreatedObjects.Add(gameObject);
		}
		return Main.SKIP_ORIGINAL;
	}
}
