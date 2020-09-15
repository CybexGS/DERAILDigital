using System;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;

namespace Cybex.DERAILDigital
{
	public static class Main
	{
		public static bool enabled;
		public static UnityModManager.ModEntry mod;

		public static bool Load(UnityModManager.ModEntry modEntry)
		{
			mod = modEntry;

			Harmony harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll();

			TabletLoader.Init();

			if (SaveLoadController.carsAndJobsLoadingFinished && WorldStreamingInit.IsLoaded)
				OnLoadingFinished();
			else
				WorldStreamingInit.LoadingFinished += OnLoadingFinished;

			return true;
		}

		static void OnLoadingFinished()
		{
			TabletLoader.CreateShopItems();
		}
	}
}
