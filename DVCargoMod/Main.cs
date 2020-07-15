using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;

namespace DVCargoMod
{
	class Main
	{
		private static UnityModManager.ModEntry modEntry;

		static void Load(UnityModManager.ModEntry modEntry)
		{
			var harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

		}
	}
}
