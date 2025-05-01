using DV.RenderTextureSystem.BookletRender;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
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

[HarmonyPatch(typeof(FrontPageTemplatePaper), nameof(FrontPageTemplatePaper.FillInData))]
class FrontPageTemplatePaper_Patch
{
	static bool Prefix(ref FrontPageTemplatePaper __instance)
	{
		if (__instance.data == null)
		{
			return Main.KEEP_ORIGINAL;
		}
		__instance.jobType.text = __instance.data.jobType;
		__instance.jobSubtype.text = __instance.data.jobSubtype;
		__instance.jobId.text = __instance.data.jobId;
		__instance.jobTypeBgColor.color = __instance.data.jobTypeColor;
		__instance.jobDescription.text = __instance.data.jobDescription;
		Image image = __instance.cargoIcon1;
		if (image != null)
		{
			image.gameObject.SetActive(false);
		}
		Image image2 = __instance.cargoIcon2;
		if (image2 != null)
		{
			image2.gameObject.SetActive(false);
		}
		__instance.singleStationName.text = __instance.data.singleStationName;
		__instance.singleStationType.text = __instance.data.singleStationType;
		__instance.singleStationBgColor.color = __instance.data.singleStationBgColor;
		__instance.startStationType.text = __instance.data.startStationType;
		__instance.startStationName.text = __instance.data.startStationName;
		__instance.startStationBgColor.color = __instance.data.startStationBgColor;
		__instance.endStationType.text = __instance.data.endStationType;
		__instance.endStationName.text = __instance.data.endStationName;
		__instance.endStationBgColor.color = __instance.data.endStationBgColor;
		if (__instance.singleStationName.text == "")
		{
			__instance.singleStationName.transform.parent.gameObject.SetActive(false);
			__instance.startStationName.transform.parent.gameObject.SetActive(true);
			__instance.endStationName.transform.parent.gameObject.SetActive(true);
		}
		else
		{
			__instance.startStationName.transform.parent.gameObject.SetActive(false);
			__instance.endStationName.transform.parent.gameObject.SetActive(false);
			__instance.singleStationName.transform.parent.gameObject.SetActive(true);
		}
		__instance.trainLength.text = __instance.data.trainLength;
		__instance.trainMass.text = __instance.data.trainMass;
		__instance.trainValue.text = __instance.data.trainValue;
		__instance.timeBonus.text = __instance.data.timeBonus;
		__instance.DisplayRequiredLicenses(__instance.data.requiredLicenses);
		__instance.payment.text = "+$" + __instance.data.payment;
		__instance.pageNumber.text = __instance.data.pageNumber + "/" + __instance.data.totalPages;
		bool flag = __instance.data.cargoTypePerCar != null;
		if (flag && __instance.data.cargoTypePerCar?.Count != __instance.data.cars.Count)
		{
			flag = false;
			Main.DebugLog(() => "Different number of cargoTypePerCar and cars! This shouldn't happen ever! Will treat it like there is no cargo!");
		}
		int num = Mathf.Min(__instance.data.cars.Count, 20);
		bool flag2 = num <= 10;
		__instance.trackLineBot.gameObject.SetActive(true);
		__instance.trackLineTop.gameObject.SetActive(!flag2);
		TrainCar[] cars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
		for (int i = 0; i < num; i++)
		{
			int num2 = i % 10;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.car, new Vector3(-41f + 112f * (float)num2, 27f, 0f), Quaternion.identity);
			gameObject.transform.localScale = new Vector3(0.67f, 0.67f, 1f);
			Image image3 = (flag2 || i >= 10) ? __instance.trackLineBot : __instance.trackLineTop;
			gameObject.transform.SetParent(image3.transform, false);
			TrainCarLivery type = __instance.data.cars[i].type;
			Sprite icon = type.icon;
			if (icon != null)
			{
				gameObject.GetComponent<Image>().sprite = icon;
			}
			gameObject.GetComponentInChildren<TextMeshProUGUI>().text = (__instance.displayCarIds ? __instance.data.cars[i].ID : "");
			if (flag)
			{
				CargoType_v2 cargoType_v = __instance.data.cargoTypePerCar![i].ToV2();
				if (cargoType_v.HasVisibleModelForCarType(type.parentType))
				{
					var changeIcon = false;
					if (type.v1 == TrainCarType.FlatbedEmpty || type.v1 == TrainCarType.FlatbedStakes)
					{
						foreach (var cargo in Cargos.ContainerizableCargos)
						{
							if (cargo == __instance.data.cargoTypePerCar[i])
							{
								changeIcon = true;
								break;
							}
						}
					}
					Sprite icon2;
					if (changeIcon)
					{
						var carID = __instance.data.cars[i].ID;
						TrainCar trainCar = cars.Where(c => c.ID == carID).First();
						if (trainCar == null)
						{
							return Main.KEEP_ORIGINAL;
						}
						if (trainCar.CargoModelController.currentCargoModel == null)
						{
							return Main.KEEP_ORIGINAL;
						}
						string currentCargoModel = trainCar.CargoModelController.currentCargoModel.name.Remove(0, 19).Replace("(Clone)", "").Replace("White", "AAG").Replace("Old", "").Replace("AC", "").Replace("Red", "Traeg");
						icon2 = CargoSprites.getContainerSprite(currentCargoModel);
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
